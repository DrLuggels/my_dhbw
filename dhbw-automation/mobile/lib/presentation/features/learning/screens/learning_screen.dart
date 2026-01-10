import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../providers/learning_provider.dart';
import '../../../../data/models/learning/exercise_model.dart';

class LearningScreen extends ConsumerWidget {
  const LearningScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final learningState = ref.watch(learningProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Übungsaufgaben'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () =>
                ref.read(learningProvider.notifier).loadDueExercises(),
          ),
        ],
      ),
      body: Column(
        children: [
          // Stats
          if (learningState.dueCount > 0)
            Container(
              padding: const EdgeInsets.all(16),
              color: Colors.purple.shade50,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.pending_actions, color: Colors.purple.shade700),
                  const SizedBox(width: 8),
                  Text(
                    '${learningState.dueCount} fällige Übungen',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: Colors.purple.shade700,
                    ),
                  ),
                ],
              ),
            ),

          // Messages
          if (learningState.successMessage != null)
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
                      learningState.successMessage!,
                      style: TextStyle(color: Colors.green.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(learningProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          if (learningState.error != null)
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
                      learningState.error!,
                      style: TextStyle(color: Colors.red.shade700),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    onPressed: () =>
                        ref.read(learningProvider.notifier).clearMessages(),
                  ),
                ],
              ),
            ),

          // Exercise List
          Expanded(
            child: learningState.isLoading
                ? const Center(child: CircularProgressIndicator())
                : learningState.exercises.isEmpty
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
                              'Keine fälligen Übungen',
                              style: TextStyle(
                                fontSize: 18,
                                color: Colors.grey.shade600,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Gut gemacht! 🎉',
                              style: TextStyle(
                                fontSize: 16,
                                color: Colors.grey.shade500,
                              ),
                            ),
                          ],
                        ),
                      )
                    : ListView.builder(
                        itemCount: learningState.subjects.length,
                        itemBuilder: (context, index) {
                          final subject = learningState.subjects[index];
                          final exercises =
                              learningState.exercisesBySubject[subject]!;

                          return _SubjectSection(
                            subject: subject,
                            exercises: exercises,
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}

class _SubjectSection extends StatelessWidget {
  final String subject;
  final List<ExerciseModel> exercises;

  const _SubjectSection({
    required this.subject,
    required this.exercises,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: Text(
            subject,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
        ...exercises.map((exercise) => _ExerciseCard(exercise: exercise)),
      ],
    );
  }
}

class _ExerciseCard extends ConsumerWidget {
  final ExerciseModel exercise;

  const _ExerciseCard({required this.exercise});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isOverdue = exercise.nextReviewDate.isBefore(DateTime.now());

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: _getDifficultyColor(exercise.difficulty),
          child: Text(
            exercise.difficulty[0].toUpperCase(),
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
        title: Text(
          exercise.questionText,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Row(
              children: [
                Icon(
                  isOverdue ? Icons.warning : Icons.schedule,
                  size: 14,
                  color: isOverdue ? Colors.orange : Colors.grey,
                ),
                const SizedBox(width: 4),
                Text(
                  isOverdue
                      ? 'Überfällig'
                      : 'Fällig: ${_formatDate(exercise.nextReviewDate)}',
                  style: TextStyle(
                    fontSize: 12,
                    color: isOverdue ? Colors.orange : Colors.grey.shade600,
                  ),
                ),
              ],
            ),
            if (exercise.reviewCount > 0) ...[
              const SizedBox(height: 4),
              Text(
                '${exercise.reviewCount}x geübt',
                style: TextStyle(
                  fontSize: 11,
                  color: Colors.grey.shade500,
                ),
              ),
            ],
          ],
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () {
          ref.read(learningProvider.notifier).setCurrentExercise(exercise);
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => _ExerciseDetailScreen(exercise: exercise),
            ),
          );
        },
      ),
    );
  }

  Color _getDifficultyColor(String difficulty) {
    switch (difficulty.toLowerCase()) {
      case 'easy':
        return Colors.green;
      case 'medium':
        return Colors.orange;
      case 'hard':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = date.difference(now).inDays;

    if (diff == 0) return 'Heute';
    if (diff == 1) return 'Morgen';
    if (diff < 7) return 'in $diff Tagen';

    return DateFormat('d.M.').format(date);
  }
}

class _ExerciseDetailScreen extends ConsumerStatefulWidget {
  final ExerciseModel exercise;

  const _ExerciseDetailScreen({required this.exercise});

  @override
  ConsumerState<_ExerciseDetailScreen> createState() =>
      _ExerciseDetailScreenState();
}

class _ExerciseDetailScreenState
    extends ConsumerState<_ExerciseDetailScreen> {
  final TextEditingController _answerController = TextEditingController();
  bool _showHint = false;
  bool _submitted = false;
  int? _selectedQuality;

  @override
  void dispose() {
    _answerController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final learningState = ref.watch(learningProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.exercise.subject),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Question
            Text(
              'Frage:',
              style: TextStyle(
                fontSize: 14,
                color: Colors.grey.shade600,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              widget.exercise.questionText,
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 24),

            // Hint
            if (widget.exercise.hint != null &&
                widget.exercise.hint!.isNotEmpty) ...[
              TextButton.icon(
                icon: Icon(_showHint ? Icons.visibility_off : Icons.lightbulb),
                label: Text(_showHint ? 'Hinweis ausblenden' : 'Hinweis anzeigen'),
                onPressed: () => setState(() => _showHint = !_showHint),
              ),
              if (_showHint) ...[
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.amber.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.amber.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.lightbulb, color: Colors.amber.shade700),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          widget.exercise.hint!,
                          style: TextStyle(color: Colors.amber.shade900),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 24),
            ],

            // Answer Input
            TextField(
              controller: _answerController,
              enabled: !_submitted,
              maxLines: 4,
              decoration: InputDecoration(
                labelText: 'Deine Antwort',
                border: const OutlineInputBorder(),
                filled: true,
                fillColor: _submitted ? Colors.grey.shade100 : null,
              ),
            ),
            const SizedBox(height: 16),

            // Submit Button
            if (!_submitted)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _answerController.text.isEmpty
                      ? null
                      : () => setState(() => _submitted = true),
                  child: const Text('Antwort überprüfen'),
                ),
              ),

            // Quality Rating (after submission)
            if (_submitted) ...[
              const SizedBox(height: 24),
              Text(
                'Wie schwer war diese Aufgabe?',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.grey.shade700,
                ),
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                children: [
                  _QualityButton(
                    label: 'Schwer',
                    quality: 2,
                    color: Colors.red,
                    selected: _selectedQuality == 2,
                    onTap: () => setState(() => _selectedQuality = 2),
                  ),
                  _QualityButton(
                    label: 'Mittel',
                    quality: 3,
                    color: Colors.orange,
                    selected: _selectedQuality == 3,
                    onTap: () => setState(() => _selectedQuality = 3),
                  ),
                  _QualityButton(
                    label: 'Einfach',
                    quality: 5,
                    color: Colors.green,
                    selected: _selectedQuality == 5,
                    onTap: () => setState(() => _selectedQuality = 5),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              // Submit Answer Button
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed:
                      _selectedQuality == null || learningState.isSubmitting
                          ? null
                          : _handleSubmit,
                  child: learningState.isSubmitting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Absenden'),
                ),
              ),
            ],

            // Stats
            const SizedBox(height: 24),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.grey.shade100,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Statistik',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      color: Colors.grey.shade700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _StatItem(
                        label: 'Durchgänge',
                        value: widget.exercise.reviewCount.toString(),
                      ),
                      _StatItem(
                        label: 'Serie',
                        value: widget.exercise.repetitions.toString(),
                      ),
                      _StatItem(
                        label: 'Schwierigkeit',
                        value: widget.exercise.difficulty,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _handleSubmit() async {
    if (_selectedQuality == null) return;

    final success = await ref.read(learningProvider.notifier).submitAnswer(
          widget.exercise.id,
          _answerController.text,
          _selectedQuality!,
        );

    if (success && mounted) {
      Navigator.pop(context);
    }
  }
}

class _QualityButton extends StatelessWidget {
  final String label;
  final int quality;
  final Color color;
  final bool selected;
  final VoidCallback onTap;

  const _QualityButton({
    required this.label,
    required this.quality,
    required this.color,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onTap(),
      selectedColor: color.withOpacity(0.3),
      labelStyle: TextStyle(
        color: selected
            ? Color.alphaBlend(Colors.black.withOpacity(0.5), color)
            : Colors.grey.shade700,
        fontWeight: selected ? FontWeight.bold : FontWeight.normal,
      ),
      side: BorderSide(
        color: selected ? color : Colors.grey.shade300,
        width: selected ? 2 : 1,
      ),
    );
  }
}

class _StatItem extends StatelessWidget {
  final String label;
  final String value;

  const _StatItem({
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          value,
          style: const TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
          ),
        ),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            color: Colors.grey.shade600,
          ),
        ),
      ],
    );
  }
}
