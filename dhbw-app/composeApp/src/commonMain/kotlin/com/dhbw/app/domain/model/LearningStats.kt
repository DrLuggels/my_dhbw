package com.dhbw.app.domain.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class LearningStats(
    @SerialName("total_entities") val totalEntities: Int,
    @SerialName("mastered_entities") val masteredEntities: Int,
    @SerialName("average_mastery") val averageMastery: Double,
    @SerialName("total_exercises") val totalExercises: Int,
    @SerialName("answered_exercises") val answeredExercises: Int,
    @SerialName("correct_exercises") val correctExercises: Int,
    val accuracy: Double,
)
