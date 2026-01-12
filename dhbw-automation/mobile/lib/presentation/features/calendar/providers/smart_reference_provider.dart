import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';
import '../../../../data/models/smart_reference/smart_reference_models.dart';
import '../../../../data/repositories/smart_reference_repository.dart';
import '../../auth/providers/auth_provider.dart';

part 'smart_reference_provider.freezed.dart';

/// Smart Reference State
@freezed
class SmartReferenceState with _$SmartReferenceState {
  const factory SmartReferenceState({
    @Default([]) List<SuggestedLink> suggestions,
    @Default([]) List<KnowledgeLink> eventLinks,
    @Default({}) Map<String, ProfessorInfo> professors,
    @Default({}) Map<String, SubjectInfo> subjects,
    SmartReferenceResult? lastResolution,
    @Default(false) bool isLoading,
    @Default(false) bool isAnalyzing,
    String? error,
    String? successMessage,
  }) = _SmartReferenceState;
}

/// Smart Reference Repository Provider
final smartReferenceRepositoryProvider = Provider<SmartReferenceRepository>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return SmartReferenceRepository(dioClient);
});

/// Smart Reference State Notifier
class SmartReferenceNotifier extends StateNotifier<SmartReferenceState> {
  final SmartReferenceRepository _repository;

  SmartReferenceNotifier(this._repository) : super(const SmartReferenceState());

  /// Resolve natural language references in text
  Future<SmartReferenceResult?> resolveReferences(String text, {DateTime? referenceDate}) async {
    state = state.copyWith(isAnalyzing: true, error: null);

    try {
      final response = await _repository.resolveReferences(
        text: text,
        referenceDate: referenceDate,
      );

      if (response.success && response.data != null) {
        state = state.copyWith(
          lastResolution: response.data,
          isAnalyzing: false,
        );
        return response.data;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler bei der Referenzauflösung',
          isAnalyzing: false,
        );
        return null;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString(),
        isAnalyzing: false,
      );
      return null;
    }
  }

  /// Get suggestions for note content
  Future<void> getSuggestions(String noteContent, {int? sourceEventId}) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _repository.getSuggestions(
        noteContent: noteContent,
        sourceEventId: sourceEventId,
      );

      if (response.success && response.data != null) {
        state = state.copyWith(
          suggestions: response.data!,
          isLoading: false,
        );
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Laden der Vorschläge',
          isLoading: false,
        );
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString(),
        isLoading: false,
      );
    }
  }

  /// Auto-link a note to related events
  Future<int> autoLinkNote(int eventId, String noteContent, {bool autoConfirm = true}) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _repository.autoLinkNote(
        eventId: eventId,
        noteContent: noteContent,
        autoConfirmHighConfidence: autoConfirm,
      );

      if (response.success && response.data != null) {
        state = state.copyWith(
          successMessage: '${response.data!.linksCreated} Verknüpfung(en) erstellt',
          isLoading: false,
        );
        return response.data!.linksCreated;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Verknüpfen',
          isLoading: false,
        );
        return 0;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString(),
        isLoading: false,
      );
      return 0;
    }
  }

  /// Confirm a suggested link
  Future<bool> confirmSuggestion(SuggestedLink suggestion) async {
    try {
      final response = await _repository.confirmLink(
        sourceType: suggestion.sourceType,
        sourceId: suggestion.sourceId,
        targetType: suggestion.targetType,
        targetId: suggestion.targetId,
        linkType: suggestion.linkType,
        confidence: suggestion.confidence,
        reason: suggestion.reason,
      );

      if (response.success) {
        // Remove from suggestions list
        state = state.copyWith(
          suggestions: state.suggestions.where((s) => s != suggestion).toList(),
          successMessage: 'Verknüpfung bestätigt',
        );
        return true;
      }
      return false;
    } catch (e) {
      state = state.copyWith(error: e.toString());
      return false;
    }
  }

  /// Load links for a specific event
  Future<void> loadLinksForEvent(int eventId) async {
    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _repository.getLinksForEvent(eventId);

      if (response.success && response.data != null) {
        state = state.copyWith(
          eventLinks: response.data!,
          isLoading: false,
        );
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Laden der Verknüpfungen',
          isLoading: false,
        );
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString(),
        isLoading: false,
      );
    }
  }

  /// Load professors index
  Future<void> loadProfessors() async {
    try {
      final response = await _repository.getProfessors();

      if (response.success && response.data != null) {
        state = state.copyWith(professors: response.data!);
      }
    } catch (e) {
      // Silent fail for background loading
    }
  }

  /// Load subjects index
  Future<void> loadSubjects() async {
    try {
      final response = await _repository.getSubjects();

      if (response.success && response.data != null) {
        state = state.copyWith(subjects: response.data!);
      }
    } catch (e) {
      // Silent fail for background loading
    }
  }

  /// Clear suggestions
  void clearSuggestions() {
    state = state.copyWith(suggestions: []);
  }

  /// Clear error
  void clearError() {
    state = state.copyWith(error: null);
  }

  /// Clear success message
  void clearSuccessMessage() {
    state = state.copyWith(successMessage: null);
  }
}

/// Smart Reference Provider
final smartReferenceProvider =
    StateNotifierProvider<SmartReferenceNotifier, SmartReferenceState>((ref) {
  final repository = ref.watch(smartReferenceRepositoryProvider);
  return SmartReferenceNotifier(repository);
});

/// Provider for suggestions count (for badges)
final suggestionsCountProvider = Provider<int>((ref) {
  final state = ref.watch(smartReferenceProvider);
  return state.suggestions.length;
});

/// Provider for high-confidence suggestions only
final highConfidenceSuggestionsProvider = Provider<List<SuggestedLink>>((ref) {
  final state = ref.watch(smartReferenceProvider);
  return state.suggestions.where((s) => s.confidence >= 0.8).toList();
});
