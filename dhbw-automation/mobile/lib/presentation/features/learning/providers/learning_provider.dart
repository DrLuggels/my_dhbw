import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/storage/secure_storage.dart';
import '../../../../data/repositories/learning_repository.dart';
import '../../../../data/models/learning/exercise_model.dart';
import '../../auth/providers/auth_provider.dart';

part 'learning_provider.freezed.dart';
part 'learning_provider.g.dart';

/// Learning State
@freezed
class LearningState with _$LearningState {
  const factory LearningState({
    @Default([]) List<ExerciseModel> exercises,
    @Default(false) bool isLoading,
    @Default(false) bool isSubmitting,
    ExerciseModel? currentExercise,
    String? error,
    String? successMessage,
  }) = _LearningState;

  const LearningState._();

  /// Get count of due exercises
  int get dueCount => exercises.length;

  /// Get exercises by subject
  Map<String, List<ExerciseModel>> get exercisesBySubject {
    final Map<String, List<ExerciseModel>> grouped = {};
    for (var exercise in exercises) {
      grouped.putIfAbsent(exercise.subject, () => []).add(exercise);
    }
    return grouped;
  }

  /// Get subjects list
  List<String> get subjects => exercisesBySubject.keys.toList();
}

/// Learning Provider
@riverpod
class Learning extends _$Learning {
  late LearningRepository _repository;

  @override
  LearningState build() {
    _repository = LearningRepository(DioClient(SecureStorage()));
    loadDueExercises();
    return const LearningState();
  }

  /// Load due exercises for current user
  Future<void> loadDueExercises() async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final userId = ref.read(authProvider).user?.id;
      if (userId == null) {
        throw Exception('Benutzer nicht angemeldet');
      }

      final response = await _repository.getDueExercises(userId);

      if (response.success && response.data != null) {
        state = state.copyWith(
          exercises: response.data!,
          isLoading: false,
          successMessage: response.message,
        );
      } else {
        state = state.copyWith(
          isLoading: false,
          error: response.message ?? 'Fehler beim Laden der Übungen',
        );
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: e.toString(),
      );
    }
  }

  /// Set current exercise for practice
  void setCurrentExercise(ExerciseModel exercise) {
    state = state.copyWith(currentExercise: exercise);
  }

  /// Submit answer with quality rating
  /// Quality: 0 = complete blackout, 5 = perfect recall
  Future<bool> submitAnswer(int exerciseId, String answer, int quality) async {
    state = state.copyWith(isSubmitting: true, error: null);

    try {
      final response = await _repository.submitAnswer(
        exerciseId,
        answer,
        quality,
      );

      if (response.success) {
        // Remove from due list if successfully answered
        final updatedExercises = state.exercises
            .where((ex) => ex.id != exerciseId)
            .toList();

        state = state.copyWith(
          exercises: updatedExercises,
          isSubmitting: false,
          currentExercise: null,
          successMessage: 'Antwort gespeichert!',
        );
        return true;
      } else {
        state = state.copyWith(
          isSubmitting: false,
          error: response.message ?? 'Fehler beim Speichern der Antwort',
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isSubmitting: false,
        error: e.toString(),
      );
      return false;
    }
  }

  /// Calculate quality based on correctness
  /// Simplified: correct = 5, incorrect = 2
  int calculateQuality(String userAnswer, String? correctAnswer) {
    if (correctAnswer == null || correctAnswer.isEmpty) {
      // No correct answer provided, assume correct
      return 5;
    }

    final userLower = userAnswer.trim().toLowerCase();
    final correctLower = correctAnswer.trim().toLowerCase();

    if (userLower == correctLower) {
      return 5; // Perfect recall
    } else if (userLower.contains(correctLower) ||
        correctLower.contains(userLower)) {
      return 3; // Partial match
    } else {
      return 2; // Incorrect but some recall
    }
  }

  /// Clear messages
  void clearMessages() {
    state = state.copyWith(
      error: null,
      successMessage: null,
    );
  }
}
