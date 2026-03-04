package com.dhbw.app.ui.learning

import cafe.adriel.voyager.core.model.ScreenModel
import cafe.adriel.voyager.core.model.screenModelScope
import com.dhbw.app.data.repository.LearningRepository
import com.dhbw.app.domain.model.AnswerRequest
import com.dhbw.app.domain.model.Exercise
import com.dhbw.app.domain.model.LearningStats
import com.dhbw.app.domain.model.Streak
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

enum class LearningPhase { OVERVIEW, EXERCISE, RESULT }

data class LearningState(
    val phase: LearningPhase = LearningPhase.OVERVIEW,
    val isLoading: Boolean = true,
    val stats: LearningStats? = null,
    val streak: Streak? = null,
    val dueCount: Int = 0,
    val exercise: Exercise? = null,
    val answeredExercise: Exercise? = null,
    val userAnswer: String = "",
    val error: String? = null,
)

class LearningViewModel(
    private val learningRepo: LearningRepository,
) : ScreenModel {

    private val _state = MutableStateFlow(LearningState())
    val state: StateFlow<LearningState> = _state

    init {
        loadOverview()
    }

    fun loadOverview() {
        screenModelScope.launch {
            _state.value = LearningState(isLoading = true)
            try {
                val stats = learningRepo.getStats().getOrNull()
                val streak = learningRepo.getStreak().getOrNull()
                val due = learningRepo.getDueCount().getOrElse { 0 }
                _state.value = LearningState(
                    phase = LearningPhase.OVERVIEW,
                    isLoading = false,
                    stats = stats,
                    streak = streak,
                    dueCount = due,
                )
            } catch (e: Exception) {
                _state.value = LearningState(isLoading = false, error = e.message)
            }
        }
    }

    fun startLearning() {
        screenModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            learningRepo.getNextExercise()
                .onSuccess { exercise ->
                    _state.value = _state.value.copy(
                        phase = LearningPhase.EXERCISE,
                        isLoading = false,
                        exercise = exercise,
                        userAnswer = "",
                    )
                }
                .onFailure { e ->
                    _state.value = _state.value.copy(
                        isLoading = false,
                        error = e.message,
                    )
                }
        }
    }

    fun updateAnswer(answer: String) {
        _state.value = _state.value.copy(userAnswer = answer)
    }

    fun submitAnswer() {
        val exercise = _state.value.exercise ?: return
        val answer = _state.value.userAnswer.takeIf { it.isNotBlank() } ?: return

        screenModelScope.launch {
            _state.value = _state.value.copy(isLoading = true)
            learningRepo.submitAnswer(exercise.id, AnswerRequest(answer, 3))
                .onSuccess { result ->
                    _state.value = _state.value.copy(
                        phase = LearningPhase.RESULT,
                        isLoading = false,
                        answeredExercise = result,
                    )
                }
                .onFailure { e ->
                    _state.value = _state.value.copy(
                        isLoading = false,
                        error = e.message,
                    )
                }
        }
    }

    fun submitRating(rating: Int) {
        val exercise = _state.value.exercise ?: return
        screenModelScope.launch {
            learningRepo.submitAnswer(
                exercise.id,
                AnswerRequest(_state.value.userAnswer, rating),
            )
            // Load next exercise
            startLearning()
        }
    }

    fun backToOverview() {
        loadOverview()
    }
}
