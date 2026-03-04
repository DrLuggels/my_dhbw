package com.dhbw.app.domain.model

import kotlinx.datetime.Instant
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class CalendarEvent(
    val id: Int,
    val title: String,
    val description: String? = null,
    @SerialName("start_time") val startTime: Instant,
    @SerialName("end_time") val endTime: Instant? = null,
    @SerialName("all_day") val allDay: Boolean,
    @SerialName("event_type") val eventType: String,
    val source: String,
    @SerialName("external_id") val externalId: String? = null,
    val subject: String? = null,
    val location: String? = null,
    @SerialName("created_at") val createdAt: Instant,
)
