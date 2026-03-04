package com.dhbw.app.ui.calendar

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.dhbw.app.domain.model.CalendarEvent
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime

@Composable
fun EventDetailDialog(event: CalendarEvent, tz: TimeZone, onDismiss: () -> Unit) {
    val startLocal = event.startTime.toLocalDateTime(tz)
    val endLocal = event.endTime?.toLocalDateTime(tz)

    val timeText = buildString {
        append(
            "${startLocal.hour.toString().padStart(2, '0')}:" +
                "${startLocal.minute.toString().padStart(2, '0')}",
        )
        if (endLocal != null) {
            append(
                " – ${endLocal.hour.toString().padStart(2, '0')}:" +
                    "${endLocal.minute.toString().padStart(2, '0')}",
            )
        }
    }
    val dateText = "${startLocal.dayOfMonth}.${startLocal.monthNumber}.${startLocal.year}"

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(text = event.title, fontWeight = FontWeight.Bold)
        },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                DetailRow("Datum", dateText)
                DetailRow("Zeit", timeText)
                event.location?.let { DetailRow("Ort", it) }
                event.subject?.let { DetailRow("Fach", it) }
                if (event.eventType.isNotBlank()) {
                    DetailRow("Typ", event.eventType)
                }
                event.description?.let {
                    if (it.isNotBlank()) {
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = it,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) { Text("Schließen") }
        },
    )
}

@Composable
private fun DetailRow(label: String, value: String) {
    Row {
        Text(
            text = "$label: ",
            fontWeight = FontWeight.SemiBold,
            style = MaterialTheme.typography.bodyMedium,
        )
        Text(text = value, style = MaterialTheme.typography.bodyMedium)
    }
}
