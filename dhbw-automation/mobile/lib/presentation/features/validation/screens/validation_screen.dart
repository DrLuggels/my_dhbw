import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/validation_provider.dart';
import '../../../../data/models/validation/staged_entity_model.dart';

class ValidationScreen extends ConsumerWidget {
  const ValidationScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final validationState = ref.watch(validationProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Rückfragen'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () =>
                ref.read(validationProvider.notifier).fetchPendingEntities(),
          ),
        ],
      ),
      body: Column(
        children: [
          // Stats
          if (validationState.pendingCount > 0)
            Container(
              padding: const EdgeInsets.all(16),
              color: Colors.blue.shade50,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  _StatChip(
                    icon: Icons.pending,
                    label: 'Ausstehend',
                    value: validationState.pendingCount.toString(),
                  ),
                  _StatChip(
                    icon: Icons.priority_high,
                    label: 'Hohe Priorität',
                    value: validationState.highPriorityCount.toString(),
                    color: Colors.orange,
                  ),
                  _StatChip(
                    icon: Icons.question_answer,
                    label: 'Mit Fragen',
                    value: validationState.withQuestionsCount.toString(),
                    color: Colors.purple,
                  ),
                ],
              ),
            ),

          // Messages
          if (validationState.successMessage != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.green.shade50,
              child: Row(
                children: [
                  Icon(Icons.check_circle, color: Colors.green.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      validationState.successMessage!,
                      style: TextStyle(color: Colors.green.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(validationProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          if (validationState.error != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: Colors.red.shade50,
              child: Row(
                children: [
                  Icon(Icons.error_outline, color: Colors.red.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      validationState.error!,
                      style: TextStyle(color: Colors.red.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(validationProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          // Entity List
          Expanded(
            child: validationState.isLoading
                ? const Center(child: CircularProgressIndicator())
                : validationState.pendingEntities.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              Icons.check_circle_outline,
                              size: 100,
                              color: Colors.grey.shade300,
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'Keine offenen Rückfragen',
                              style: TextStyle(
                                fontSize: 18,
                                color: Colors.grey.shade600,
                              ),
                            ),
                          ],
                        ),
                      )
                    : ListView.builder(
                        itemCount: validationState.pendingEntities.length,
                        itemBuilder: (context, index) {
                          final entity = validationState.pendingEntities[index];
                          return _EntityCard(entity: entity);
                        },
                      ),
          ),
        ],
      ),
    );
  }
}

class _StatChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? color;

  const _StatChip({
    required this.icon,
    required this.label,
    required this.value,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, color: color ?? Colors.blue),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
            color: color ?? Colors.blue,
          ),
        ),
        Text(
          label,
          style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
        ),
      ],
    );
  }
}

class _EntityCard extends ConsumerWidget {
  final StagedEntityModel entity;

  const _EntityCard({required this.entity});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Card(
      margin: const EdgeInsets.all(8),
      child: ExpansionTile(
        leading: _getIcon(entity.entityType),
        title: Text(
          _getTitle(entity.entityType),
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                _ConfidenceChip(score: entity.confidenceScore),
                const SizedBox(width: 8),
                _PriorityChip(priority: entity.priority),
              ],
            ),
            if (entity.questions.isNotEmpty)
              Text('${entity.questions.length} Fragen'),
          ],
        ),
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Entity Data Preview
                Text(
                  'Daten:',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.grey.shade700,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  _formatEntityData(entity.entityData),
                  style: TextStyle(color: Colors.grey.shade600),
                ),
                const SizedBox(height: 16),

                // Actions
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton.icon(
                      icon: const Icon(Icons.close),
                      label: const Text('Ablehnen'),
                      onPressed: () => _handleReject(context, ref),
                      style: TextButton.styleFrom(foregroundColor: Colors.red),
                    ),
                    const SizedBox(width: 8),
                    ElevatedButton.icon(
                      icon: const Icon(Icons.check),
                      label: const Text('Bestätigen'),
                      onPressed: () => _handleConfirm(context, ref),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _getIcon(String entityType) {
    IconData icon;
    Color color;

    switch (entityType) {
      case 'todo':
        icon = Icons.task_alt;
        color = Colors.blue;
        break;
      case 'meeting':
        icon = Icons.event;
        color = Colors.orange;
        break;
      case 'project':
        icon = Icons.work;
        color = Colors.purple;
        break;
      case 'learning_deficit':
        icon = Icons.school;
        color = Colors.red;
        break;
      default:
        icon = Icons.info;
        color = Colors.grey;
    }

    return Icon(icon, color: color);
  }

  String _getTitle(String entityType) {
    switch (entityType) {
      case 'todo':
        return 'Aufgabe';
      case 'meeting':
        return 'Termin';
      case 'project':
        return 'Projekt';
      case 'learning_deficit':
        return 'Lerndefizit';
      default:
        return entityType;
    }
  }

  String _formatEntityData(String jsonData) {
    try {
      final data = jsonDecode(jsonData);
      if (data is Map) {
        return data.entries
            .take(3)
            .map((e) => '${e.key}: ${e.value}')
            .join('\n');
      }
      return jsonData;
    } catch (e) {
      return jsonData;
    }
  }

  Future<void> _handleConfirm(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Entität bestätigen?'),
        content: const Text(
          'Möchtest du diese Entität bestätigen und freigeben?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Abbrechen'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Bestätigen'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await ref.read(validationProvider.notifier).confirmEntity(entity.id);
    }
  }

  Future<void> _handleReject(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Entität ablehnen?'),
        content: const Text(
          'Möchtest du diese Entität wirklich ablehnen?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Ablehnen'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await ref.read(validationProvider.notifier).rejectEntity(entity.id);
    }
  }
}

class _ConfidenceChip extends StatelessWidget {
  final int score;

  const _ConfidenceChip({required this.score});

  @override
  Widget build(BuildContext context) {
    Color color;
    if (score >= 90) {
      color = Colors.green;
    } else if (score >= 70) {
      color = Colors.orange;
    } else {
      color = Colors.red;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color),
      ),
      child: Text(
        '$score% Confidence',
        style: TextStyle(
          color: color.shade700,
          fontSize: 12,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}

class _PriorityChip extends StatelessWidget {
  final String priority;

  const _PriorityChip({required this.priority});

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (priority) {
      case 'urgent':
        color = Colors.red;
        break;
      case 'high':
        color = Colors.orange;
        break;
      case 'medium':
        color = Colors.blue;
        break;
      default:
        color = Colors.grey;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color),
      ),
      child: Text(
        priority.toUpperCase(),
        style: TextStyle(
          color: color.shade700,
          fontSize: 12,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}
