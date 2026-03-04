package com.dhbw.app.data.repository

import com.dhbw.app.data.remote.CalendarApi
import com.dhbw.app.domain.model.CalendarEvent

class CalendarRepository(private val api: CalendarApi) {

    // TODO: Add SQLDelight DAO for offline cache once driver is wired

    suspend fun getEvents(start: String, end: String): Result<List<CalendarEvent>> = runCatching {
        val response = api.getEvents(start, end)
        if (response.success && response.data != null) {
            response.data
        } else {
            error(response.message.ifBlank { "Kalender nicht verfügbar" })
        }
    }

    suspend fun syncRapla(): Result<Int> = runCatching {
        val response = api.syncRapla()
        if (response.success && response.data != null) {
            response.data["synced"] ?: 0
        } else {
            error(response.message.ifBlank { "Rapla-Sync fehlgeschlagen" })
        }
    }
}
