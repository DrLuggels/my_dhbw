package com.dhbw.app.domain.model

import kotlinx.datetime.Instant
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class Exercise(
    val id: Int,
    @SerialName("entity_id") val entityId: Int,
    val question: String,
    @SerialName("exercise_type") val exerciseType: ExerciseType,
    val difficulty: String,
    @SerialName("bloom_level") val bloomLevel: Int,
    @SerialName("options_json") val optionsJson: ExerciseOptions? = null,
    @SerialName("is_answered") val isAnswered: Boolean,
    @SerialName("is_correct") val isCorrect: Boolean? = null,
    @SerialName("correct_answer") val correctAnswer: String? = null,
    val explanation: String? = null,
    @SerialName("user_answer") val userAnswer: String? = null,
    val score: Double? = null,
    @SerialName("created_at") val createdAt: Instant,
    @SerialName("answered_at") val answeredAt: Instant? = null,
)

@Serializable
enum class ExerciseType {
    @SerialName("multiple_choice") MULTIPLE_CHOICE,
    @SerialName("fill_in_blank") FILL_IN_BLANK,
    @SerialName("free_text") FREE_TEXT,
}

@Serializable
data class ExerciseOptions(
    val options: List<String> = emptyList(),
)

@Serializable
data class AnswerRequest(
    @SerialName("user_answer") val userAnswer: String,
    val rating: Int, // 1=Again, 2=Hard, 3=Good, 4=Easy
)
