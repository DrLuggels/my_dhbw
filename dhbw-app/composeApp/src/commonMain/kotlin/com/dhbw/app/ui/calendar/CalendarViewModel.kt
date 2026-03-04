package com.dhbw.app.ui.calendar

import cafe.adriel.voyager.core.model.ScreenModel
import cafe.adriel.voyager.core.model.screenModelScope
import com.dhbw.app.data.repository.CalendarRepository
import com.dhbw.app.domain.model.CalendarEvent
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.DayOfWeek
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.minus
import kotlinx.datetime.plus
import kotlinx.datetime.toLocalDateTime

data class CalendarState(
    val isLoading: Boolean = true,
    val events: List<CalendarEvent> = emptyList(),
    val weekStart: LocalDate = currentWeekStart(),
    val isSyncing: Boolean = false,
    val error: String? = null,
)

class CalendarViewModel(
    private val calendarRepo: CalendarRepository,
) : ScreenModel {

    private val _state = MutableStateFlow(CalendarState())
    val state: StateFlow<CalendarState> = _state

    init {
        loadWeek()
    }

    fun loadWeek() {
        screenModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            val start = _state.value.weekStart.toString()
            val end = _state.value.weekStart.plus(6, DateTimeUnit.DAY).toString()

            calendarRepo.getEvents(start, end)
                .onSuccess { events ->
                    _state.value = _state.value.copy(isLoading = false, events = events)
                }
                .onFailure { e ->
                    _state.value = _state.value.copy(isLoading = false, error = e.message)
                }
        }
    }

    fun previousWeek() {
        _state.value = _state.value.copy(
            weekStart = _state.value.weekStart.minus(7, DateTimeUnit.DAY),
        )
        loadWeek()
    }

    fun nextWeek() {
        _state.value = _state.value.copy(
            weekStart = _state.value.weekStart.plus(7, DateTimeUnit.DAY),
        )
        loadWeek()
    }

    fun goToToday() {
        _state.value = _state.value.copy(weekStart = currentWeekStart())
        loadWeek()
    }

    fun syncRapla() {
        screenModelScope.launch {
            _state.value = _state.value.copy(isSyncing = true)
            calendarRepo.syncRapla()
            _state.value = _state.value.copy(isSyncing = false)
            loadWeek()
        }
    }
}

private fun currentWeekStart(): LocalDate {
    val today = Clock.System.now().toLocalDateTime(TimeZone.currentSystemDefault()).date
    val daysFromMonday = today.dayOfWeek.ordinal - DayOfWeek.MONDAY.ordinal
    return today.minus(daysFromMonday, DateTimeUnit.DAY)
}
