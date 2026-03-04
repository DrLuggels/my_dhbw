package com.dhbw.app.data.remote

import com.dhbw.app.domain.model.AnswerRequest
import com.dhbw.app.domain.model.Exercise
import com.dhbw.app.domain.model.LearningStats
import com.dhbw.app.domain.model.Streak
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.post
import io.ktor.client.request.setBody

class LearningApi(private val client: HttpClient) {

    suspend fun getNextExercise(): ApiResponse<Exercise> =
        client.get("/api/learning/next").body()

    suspend fun submitAnswer(exerciseId: Int, answer: AnswerRequest): ApiResponse<Exercise> =
        client.post("/api/learning/exercise/$exerciseId/answer") {
            setBody(answer)
        }.body()

    suspend fun getStats(): ApiResponse<LearningStats> =
        client.get("/api/learning/stats").body()

    suspend fun getStreak(): ApiResponse<Streak> =
        client.get("/api/learning/streak").body()

    suspend fun getDueCount(): ApiResponse<Int> =
        client.get("/api/learning/due").body()
}
