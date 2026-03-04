package com.dhbw.app.data.remote

import com.dhbw.app.domain.model.CalendarEvent
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.request.post

class CalendarApi(private val client: HttpClient) {

    suspend fun getEvents(start: String, end: String): ApiResponse<List<CalendarEvent>> =
        client.get("/api/calendar/events") {
            parameter("start", start)
            parameter("end", end)
        }.body()

    suspend fun syncRapla(): ApiResponse<Map<String, Int>> =
        client.post("/api/calendar/sync-rapla").body()
}
