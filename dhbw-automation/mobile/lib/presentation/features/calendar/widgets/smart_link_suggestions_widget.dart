import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../data/models/smart_reference/smart_reference_models.dart';
import '../providers/smart_reference_provider.dart';

/// Widget to display and manage smart link suggestions
class SmartLinkSuggestionsWidget extends ConsumerWidget {
  final int? sourceEventId;
  final String noteContent;
  final VoidCallback? onLinkCreated;

  const SmartLinkSuggestionsWidget({
    super.key,
    this.sourceEventId,
    required this.noteContent,
    this.onLinkCreated,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(smartReferenceProvider);
    final notifier = ref.read(smartReferenceProvider.notifier);

    // Load suggestions when widget builds
    if (state.suggestions.isEmpty && !state.isLoading && noteContent.isNotEmpty) {
      Future.microtask(() => notifier.getSuggestions(noteContent, sourceEventId: sourceEventId));
    }

    if (state.isLoading) {
      return const Padding(
        padding: EdgeInsets.all(16.0),
        child: Center(
          child: SizedBox(
            width: 24,
            height: 24,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }

    if (state.suggestions.isEmpty) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
          child: Row(
            children: [
              Icon(Icons.auto_awesome, size: 18, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 8),
              Text(
                'Erkannte Verknüpfungen',
                style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const Spacer(),
              Text(
                '${state.suggestions.length}',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ),
        ListView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: state.suggestions.length,
          itemBuilder: (context, index) {
            final suggestion = state.suggestions[index];
            return _SuggestionCard(
              suggestion: suggestion,
              onConfirm: () async {
                final success = await notifier.confirmSuggestion(suggestion);
                if (success) {
                  onLinkCreated?.call();
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Verknüpfung erstellt')),
                    );
                  }
                }
              },
              onDismiss: () {
                notifier.clearSuggestions();
              },
            );
          },
        ),
      ],
    );
  }
}

class _SuggestionCard extends StatelessWidget {
  final SuggestedLink suggestion;
  final VoidCallback onConfirm;
  final VoidCallback onDismiss;

  const _SuggestionCard({
    required this.suggestion,
    required this.onConfirm,
    required this.onDismiss,
  });

  @override
  Widget build(BuildContext context) {
    final confidenceColor = _getConfidenceColor(suggestion.confidence);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 4.0),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(_getLinkTypeIcon(suggestion.linkType), size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    suggestion.targetDisplayName ?? 'Unbekannt',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: confidenceColor.withOpacity(0.2),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    '${(suggestion.confidence * 100).toInt()}%',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: confidenceColor,
                    ),
                  ),
                ),
              ],
            ),
            if (suggestion.reason.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                suggestion.reason,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
            if (suggestion.referenceText != null) ...[
              const SizedBox(height: 4),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  '"${suggestion.referenceText}"',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    fontStyle: FontStyle.italic,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: onDismiss,
                  child: const Text('Ablehnen'),
                ),
                const SizedBox(width: 8),
                FilledButton.icon(
                  onPressed: onConfirm,
                  icon: const Icon(Icons.link, size: 18),
                  label: const Text('Verknüpfen'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Color _getConfidenceColor(double confidence) {
    if (confidence >= 0.8) return Colors.green;
    if (confidence >= 0.5) return Colors.orange;
    return Colors.red;
  }

  IconData _getLinkTypeIcon(String linkType) {
    switch (linkType) {
      case 'professor_reference':
        return Icons.person;
      case 'subject_reference':
        return Icons.school;
      case 'temporal_reference':
        return Icons.schedule;
      case 'professor_temporal_reference':
        return Icons.person_pin_circle;
      default:
        return Icons.link;
    }
  }
}

/// Compact chip for showing suggestions count
class SmartLinkBadge extends ConsumerWidget {
  final VoidCallback? onTap;

  const SmartLinkBadge({super.key, this.onTap});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final count = ref.watch(suggestionsCountProvider);

    if (count == 0) return const SizedBox.shrink();

    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.primaryContainer,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.auto_awesome,
              size: 14,
              color: Theme.of(context).colorScheme.onPrimaryContainer,
            ),
            const SizedBox(width: 4),
            Text(
              '$count',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: Theme.of(context).colorScheme.onPrimaryContainer,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Inline widget for showing resolved references while typing
class SmartReferencePreview extends ConsumerWidget {
  final String text;

  const SmartReferencePreview({super.key, required this.text});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(smartReferenceProvider);

    if (state.lastResolution == null || state.lastResolution!.references.isEmpty) {
      return const SizedBox.shrink();
    }

    return Wrap(
      spacing: 8,
      runSpacing: 4,
      children: state.lastResolution!.references.map((ref) {
        return Chip(
          avatar: Icon(_getReferenceIcon(ref.referenceType), size: 16),
          label: Text(
            ref.resolvedTo?.displayName ?? ref.originalText,
            style: const TextStyle(fontSize: 12),
          ),
          backgroundColor: Theme.of(context).colorScheme.secondaryContainer,
          padding: EdgeInsets.zero,
          visualDensity: VisualDensity.compact,
        );
      }).toList(),
    );
  }

  IconData _getReferenceIcon(String type) {
    switch (type) {
      case 'professor':
        return Icons.person;
      case 'subject':
        return Icons.school;
      case 'event':
        return Icons.event;
      case 'temporal':
        return Icons.schedule;
      case 'professorTemporal':
        return Icons.person_pin_circle;
      default:
        return Icons.link;
    }
  }
}
