package com.dhbw.app.ui.dashboard

import cafe.adriel.voyager.core.model.ScreenModel
import cafe.adriel.voyager.core.model.screenModelScope
import com.dhbw.app.data.repository.LearningRepository
import com.dhbw.app.domain.model.LearningStats
import com.dhbw.app.domain.model.Streak
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class DashboardState(
    val isLoading: Boolean = true,
    val stats: LearningStats? = null,
    val streak: Streak? = null,
    val dueCount: Int = 0,
    val error: String? = null,
)

class DashboardViewModel(
    private val learningRepo: LearningRepository,
) : ScreenModel {

    private val _state = MutableStateFlow(DashboardState())
    val state: StateFlow<DashboardState> = _state

    init {
        loadDashboard()
    }

    fun loadDashboard() {
        screenModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                val stats = learningRepo.getStats().getOrNull()
                val streak = learningRepo.getStreak().getOrNull()
                val due = learningRepo.getDueCount().getOrElse { 0 }
                _state.value = DashboardState(
                    isLoading = false,
                    stats = stats,
                    streak = streak,
                    dueCount = due,
                )
            } catch (e: Exception) {
                _state.value = DashboardState(
                    isLoading = false,
                    error = e.message ?: "Fehler beim Laden",
                )
            }
        }
    }
}
