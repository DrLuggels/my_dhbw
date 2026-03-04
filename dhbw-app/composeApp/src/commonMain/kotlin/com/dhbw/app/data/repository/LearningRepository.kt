package com.dhbw.app.data.repository

import com.dhbw.app.data.remote.LearningApi
import com.dhbw.app.domain.model.AnswerRequest
import com.dhbw.app.domain.model.Exercise
import com.dhbw.app.domain.model.LearningStats
import com.dhbw.app.domain.model.Streak

class LearningRepository(private val api: LearningApi) {

    // TODO: Add SQLDelight DAO for offline cache once driver is wired

    suspend fun getNextExercise(): Result<Exercise> = runCatching {
        val response = api.getNextExercise()
        if (response.success && response.data != null) {
            response.data
        } else {
            error(response.message.ifBlank { "Keine Übung verfügbar" })
        }
    }

    suspend fun submitAnswer(exerciseId: Int, answer: AnswerRequest): Result<Exercise> = runCatching {
        val response = api.submitAnswer(exerciseId, answer)
        if (response.success && response.data != null) {
            response.data
        } else {
            error(response.message.ifBlank { "Antwort konnte nicht gesendet werden" })
        }
    }

    suspend fun getStats(): Result<LearningStats> = runCatching {
        val response = api.getStats()
        if (response.success && response.data != null) {
            response.data
        } else {
            error(response.message.ifBlank { "Statistiken nicht verfügbar" })
        }
    }

    suspend fun getStreak(): Result<Streak> = runCatching {
        val response = api.getStreak()
        if (response.success && response.data != null) {
            response.data
        } else {
            error(response.message.ifBlank { "Streak nicht verfügbar" })
        }
    }

    suspend fun getDueCount(): Result<Int> = runCatching {
        val response = api.getDueCount()
        if (response.success && response.data != null) {
            response.data
        } else {
            0
        }
    }
}
