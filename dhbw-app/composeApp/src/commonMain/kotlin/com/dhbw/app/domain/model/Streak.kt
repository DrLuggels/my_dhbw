package com.dhbw.app.domain.model

import kotlinx.datetime.LocalDate
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class Streak(
    @SerialName("current_streak") val currentStreak: Int,
    @SerialName("longest_streak") val longestStreak: Int,
    @SerialName("last_activity_date") val lastActivityDate: LocalDate? = null,
    @SerialName("total_active_days") val totalActiveDays: Int,
    val multiplier: Double = 1.0,
)
