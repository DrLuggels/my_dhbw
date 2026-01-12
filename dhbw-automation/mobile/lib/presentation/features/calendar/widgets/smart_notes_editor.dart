import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../data/models/calendar/calendar_event_model.dart';
import '../providers/smart_reference_provider.dart';
import '../providers/calendar_provider.dart';
import 'smart_link_suggestions_widget.dart';

/// Smart Notes Editor with auto-linking capabilities
/// Analyzes notes in real-time and suggests relevant links
class SmartNotesEditor extends ConsumerStatefulWidget {
  final CalendarEventModel event;
  final Function(String notes)? onNotesChanged;
  final bool autoAnalyze;

  const SmartNotesEditor({
    super.key,
    required this.event,
    this.onNotesChanged,
    this.autoAnalyze = true,
  });

  @override
  ConsumerState<SmartNotesEditor> createState() => _SmartNotesEditorState();
}

class _SmartNotesEditorState extends ConsumerState<SmartNotesEditor> {
  late TextEditingController _controller;
  Timer? _debounceTimer;
  bool _isEditing = false;
  bool _showSuggestions = false;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.event.notes ?? '');
  }

  @override
  void dispose() {
    _controller.dispose();
    _debounceTimer?.cancel();
    super.dispose();
  }

  void _onTextChanged(String text) {
    widget.onNotesChanged?.call(text);

    // Debounce analysis to avoid too many API calls
    _debounceTimer?.cancel();
    if (widget.autoAnalyze && text.length > 10) {
      _debounceTimer = Timer(const Duration(milliseconds: 800), () {
        _analyzeText(text);
      });
    }
  }

  Future<void> _analyzeText(String text) async {
    final notifier = ref.read(smartReferenceProvider.notifier);
    await notifier.resolveReferences(text);
    await notifier.getSuggestions(text, sourceEventId: widget.event.id);

    if (mounted) {
      setState(() {
        _showSuggestions = ref.read(smartReferenceProvider).suggestions.isNotEmpty;
      });
    }
  }

  Future<void> _autoLink() async {
    final text = _controller.text;
    if (text.isEmpty) return;

    final notifier = ref.read(smartReferenceProvider.notifier);
    final linksCreated = await notifier.autoLinkNote(
      widget.event.id,
      text,
    );

    if (mounted && linksCreated > 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$linksCreated Verknüpfung(en) erstellt')),
      );
    }
  }

  Future<void> _saveNotes() async {
    final calendarNotifier = ref.read(calendarProvider.notifier);
    await calendarNotifier.updateEventNotes(widget.event.id, _controller.text);

    // Auto-link after saving
    if (widget.autoAnalyze) {
      await _autoLink();
    }

    setState(() {
      _isEditing = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final smartState = ref.watch(smartReferenceProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Header
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
          child: Row(
            children: [
              Icon(Icons.note_alt, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 8),
              Text(
                'Notizen',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const Spacer(),
              if (smartState.suggestions.isNotEmpty)
                SmartLinkBadge(
                  onTap: () => setState(() => _showSuggestions = !_showSuggestions),
                ),
              if (_isEditing) ...[
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.check),
                  onPressed: _saveNotes,
                  tooltip: 'Speichern',
                ),
              ],
            ],
          ),
        ),

        // Notes Editor
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0),
          child: GestureDetector(
            onTap: () => setState(() => _isEditing = true),
            child: _isEditing
                ? TextField(
                    controller: _controller,
                    onChanged: _onTextChanged,
                    maxLines: null,
                    minLines: 3,
                    decoration: InputDecoration(
                      hintText: 'Notizen hinzufügen...\n\nTipp: Schreibe z.B. "Prof. Müller erklärt gut" für automatische Verknüpfung',
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                      filled: true,
                      fillColor: Theme.of(context).colorScheme.surfaceContainerLowest,
                    ),
                    autofocus: true,
                  )
                : Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.surfaceContainerLowest,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                        color: Theme.of(context).colorScheme.outlineVariant,
                      ),
                    ),
                    child: Text(
                      widget.event.notes?.isNotEmpty == true
                          ? widget.event.notes!
                          : 'Tippen um Notizen hinzuzufügen...',
                      style: TextStyle(
                        color: widget.event.notes?.isNotEmpty == true
                            ? null
                            : Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
          ),
        ),

        // Real-time reference preview
        if (smartState.isAnalyzing)
          const Padding(
            padding: EdgeInsets.all(16.0),
            child: Center(
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          )
        else if (smartState.lastResolution?.references.isNotEmpty == true)
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: SmartReferencePreview(text: _controller.text),
          ),

        // Suggestions
        if (_showSuggestions && smartState.suggestions.isNotEmpty)
          SmartLinkSuggestionsWidget(
            sourceEventId: widget.event.id,
            noteContent: _controller.text,
            onLinkCreated: () {
              // Refresh event links
              ref.read(smartReferenceProvider.notifier).loadLinksForEvent(widget.event.id);
            },
          ),

        // Existing links
        if (smartState.eventLinks.isNotEmpty) ...[
          const Divider(),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
            child: Row(
              children: [
                Icon(Icons.link, size: 18, color: Theme.of(context).colorScheme.secondary),
                const SizedBox(width: 8),
                Text(
                  'Verknüpfte Inhalte',
                  style: Theme.of(context).textTheme.titleSmall,
                ),
              ],
            ),
          ),
          ...smartState.eventLinks.map((link) => _LinkedContentTile(link: link)),
        ],
      ],
    );
  }
}

class _LinkedContentTile extends StatelessWidget {
  final dynamic link; // KnowledgeLink

  const _LinkedContentTile({required this.link});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(_getEntityIcon(link.targetType)),
      title: Text(link.description ?? 'Verknüpfung'),
      subtitle: Text(_getLinkTypeLabel(link.linkType)),
      trailing: Container(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(
          '${(link.strength * 100).toInt()}%',
          style: const TextStyle(fontSize: 11),
        ),
      ),
      dense: true,
    );
  }

  IconData _getEntityIcon(String entityType) {
    switch (entityType) {
      case 'calendar_event':
        return Icons.event;
      case 'document':
        return Icons.description;
      case 'note':
        return Icons.note;
      default:
        return Icons.link;
    }
  }

  String _getLinkTypeLabel(String linkType) {
    switch (linkType) {
      case 'professor_reference':
        return 'Professor-Referenz';
      case 'subject_reference':
        return 'Fach-Referenz';
      case 'temporal_reference':
        return 'Zeit-Referenz';
      case 'professor_temporal_reference':
        return 'Prof + Zeit';
      case 'related':
        return 'Verwandt';
      default:
        return linkType;
    }
  }
}

/// Quick action button for auto-linking
class AutoLinkButton extends ConsumerWidget {
  final int eventId;
  final String noteContent;

  const AutoLinkButton({
    super.key,
    required this.eventId,
    required this.noteContent,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(smartReferenceProvider);

    return FilledButton.tonalIcon(
      onPressed: state.isLoading
          ? null
          : () async {
              final notifier = ref.read(smartReferenceProvider.notifier);
              final count = await notifier.autoLinkNote(eventId, noteContent);
              if (context.mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text('$count Verknüpfung(en) erstellt')),
                );
              }
            },
      icon: state.isLoading
          ? const SizedBox(
              width: 18,
              height: 18,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.auto_fix_high),
      label: const Text('Auto-Verknüpfen'),
    );
  }
}
