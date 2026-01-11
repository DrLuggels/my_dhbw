import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';
import '../../../../data/models/calendar/calendar_event_model.dart';
import '../../../../data/repositories/calendar_repository.dart';
import '../../auth/providers/auth_provider.dart';

part 'calendar_provider.freezed.dart';

/// Calendar State
@freezed
class CalendarState with _$CalendarState {
  const factory CalendarState({
    @Default([]) List<CalendarEventModel> events,
    @Default(false) bool isLoading,
    @Default(false) bool isSyncing,
    DateTime? selectedWeekStart,
    String? error,
    String? successMessage,
  }) = _CalendarState;

  const CalendarState._();

  /// Get Monday of current or selected week
  DateTime get weekStart {
    if (selectedWeekStart != null) return selectedWeekStart!;
    return _getMonday(DateTime.now());
  }

  /// Get Sunday of current week
  DateTime get weekEnd => weekStart.add(const Duration(days: 7));

  /// Get events for the current week
  List<CalendarEventModel> get weekEvents {
    return events.where((event) {
      // Use >= weekStart and < weekEnd (inclusive start, exclusive end)
      return !event.startTime.isBefore(weekStart) &&
          event.startTime.isBefore(weekEnd);
    }).toList();
  }

  /// Get week number (ISO 8601)
  int get weekNumber {
    final dayOfYear = weekStart.difference(DateTime(weekStart.year, 1, 1)).inDays;
    return ((dayOfYear + DateTime(weekStart.year, 1, 1).weekday) / 7).ceil();
  }

  /// Calculate Monday for given date
  static DateTime _getMonday(DateTime date) {
    final weekday = date.weekday;
    return date.subtract(Duration(days: weekday - 1));
  }
}

/// Calendar Repository Provider
final calendarRepositoryProvider = Provider<CalendarRepository>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return CalendarRepository(dioClient);
});

/// Calendar State Notifier
class CalendarNotifier extends StateNotifier<CalendarState> {
  final CalendarRepository _calendarRepository;
  final int? _userId;

  CalendarNotifier(this._calendarRepository, this._userId)
      : super(const CalendarState()) {
    // Load events on init
    if (_userId != null) {
      loadWeekEvents();
    }
  }

  /// Load events for the current week
  Future<void> loadWeekEvents() async {
    if (_userId == null) return;

    state = state.copyWith(isLoading: true, error: null);

    try {
      final response = await _calendarRepository.getEvents(
        _userId!,
        startDate: state.weekStart,
        endDate: state.weekEnd,
      );

      if (response.success && response.data != null) {
        state = state.copyWith(
          events: response.data!,
          isLoading: false,
          successMessage: response.message,
        );
      } else {
        state = state.copyWith(
          error: response.message ?? 'Fehler beim Laden der Events',
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

  /// Navigate to previous week
  Future<void> previousWeek() async {
    final newWeekStart = state.weekStart.subtract(const Duration(days: 7));
    state = state.copyWith(selectedWeekStart: newWeekStart);
    await loadWeekEvents();
  }

  /// Navigate to next week
  Future<void> nextWeek() async {
    final newWeekStart = state.weekStart.add(const Duration(days: 7));
    state = state.copyWith(selectedWeekStart: newWeekStart);
    await loadWeekEvents();
  }

  /// Navigate to today
  Future<void> goToToday() async {
    state = state.copyWith(selectedWeekStart: null);
    await loadWeekEvents();
  }

  /// Sync Rapla calendar
  Future<bool> syncRapla() async {
    if (_userId == null) return false;

    state = state.copyWith(isSyncing: true, error: null);

    try {
      final response = await _calendarRepository.syncRapla(_userId!);

      if (response.success) {
        state = state.copyWith(
          isSyncing: false,
          successMessage:
              '${response.data?['newEvents'] ?? 0} neue Events synchronisiert',
        );

        // Reload events
        await loadWeekEvents();
        return true;
      } else {
        state = state.copyWith(
          error: response.message ?? 'Sync fehlgeschlagen',
          isSyncing: false,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        error: e.toString().replaceAll('Exception: ', ''),
        isSyncing: false,
      );
      return false;
    }
  }

  /// Clear messages
  void clearMessages() {
    state = state.copyWith(error: null, successMessage: null);
  }
}

/// Calendar State Provider
final calendarProvider =
    StateNotifierProvider<CalendarNotifier, CalendarState>((ref) {
  final calendarRepository = ref.watch(calendarRepositoryProvider);
  final userId = ref.watch(authProvider).user?.id;
  return CalendarNotifier(calendarRepository, userId);
});
