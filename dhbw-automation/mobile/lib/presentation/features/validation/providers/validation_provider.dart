import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';
import '../../../../data/models/validation/staged_entity_model.dart';
import '../../../../data/repositories/validation_repository.dart';
import '../../auth/providers/auth_provider.dart';

part 'validation_provider.freezed.dart';

/// Validation State
@freezed
class ValidationState with _$ValidationState {
  const factory ValidationState({
    @Default([]) List<StagedEntityModel> pendingEntities,
    @Default(false) bool isLoading,
    String? error,
    String? successMessage,
  }) = _ValidationState;

  const ValidationState._();

  int get pendingCount => pendingEntities.length;

  int get highPriorityCount => pendingEntities
      .where((e) => e.priority == 'high' || e.priority == 'urgent')
      .length;

  int get withQuestionsCount => pendingEntities
      .where((e) => e.questions.isNotEmpty)
      .length;
}

/// Validation Repository Provider
final validationRepositoryProvider = Provider<ValidationRepository>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return ValidationRepository(dioClient);
});

/// Validation State Notifier
class ValidationNotifier extends StateNotifier<ValidationState> {
  final ValidationRepository _validationRepository;

  ValidationNotifier(this._validationRepository) : super(const ValidationState()) {
    fetchPendingEntities();
  }

  Future<void> fetchPendingEntities({String? status}) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _validationRepository.getPendingEntities(status: status);

      if (response.success && response.data != null) {
        state = state.copyWith(
          pendingEntities: response.data!,
          isLoading: false,
        );
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Laden',
          isLoading: false,
        );
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isLoading: false,
      );
    }
  }

  Future<bool> answerQuestions(int id, Map<String, String> answers) async {
    try {
      await _validationRepository.answerQuestions(id, answers);

      // Update local state
      final entities = [...state.pendingEntities];
      final index = entities.indexWhere((e) => e.id == id);
      if (index != -1) {
        final entity = entities[index];
        final updatedQuestions = entity.questions.map((q) {
          if (answers.containsKey(q.fieldName)) {
            return q.copyWith(
              isAnswered: true,
              userAnswer: answers[q.fieldName],
              answeredAt: DateTime.now().toIso8601String(),
            );
          }
          return q;
        }).toList();

        entities[index] = entity.copyWith(questions: updatedQuestions);
        state = state.copyWith(pendingEntities: entities);
      }

      return true;
    } catch (e) {
      state = state.copyWith(error: e.toString().replaceAll('Exception: ', ''));
      return false;
    }
  }

  Future<bool> confirmEntity(int id, {String? userNotes}) async {
    try {
      await _validationRepository.confirmEntity(id, userNotes: userNotes);

      // Remove from list
      state = state.copyWith(
        pendingEntities: state.pendingEntities.where((e) => e.id != id).toList(),
        successMessage: 'Entität bestätigt',
      );

      return true;
    } catch (e) {
      state = state.copyWith(error: e.toString().replaceAll('Exception: ', ''));
      return false;
    }
  }

  Future<bool> rejectEntity(int id, {String? reason}) async {
    try {
      await _validationRepository.rejectEntity(id, reason: reason);

      // Remove from list
      state = state.copyWith(
        pendingEntities: state.pendingEntities.where((e) => e.id != id).toList(),
        successMessage: 'Entität abgelehnt',
      );

      return true;
    } catch (e) {
      state = state.copyWith(error: e.toString().replaceAll('Exception: ', ''));
      return false;
    }
  }

  void clearMessages() {
    state = state.copyWith(error: null, successMessage: null);
  }
}

/// Validation State Provider
final validationProvider = StateNotifierProvider<ValidationNotifier, ValidationState>((ref) {
  final validationRepository = ref.watch(validationRepositoryProvider);
  return ValidationNotifier(validationRepository);
});
