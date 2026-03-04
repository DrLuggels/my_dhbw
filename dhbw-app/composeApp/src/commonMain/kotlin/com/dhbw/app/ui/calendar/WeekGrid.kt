package com.dhbw.app.ui.calendar

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.dhbw.app.domain.model.CalendarEvent
import com.dhbw.app.ui.theme.EventExam
import com.dhbw.app.ui.theme.EventSelfStudy
import com.dhbw.app.ui.theme.EventTutorium
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.toLocalDateTime

private val HOUR_HEIGHT = 60.dp
private val DAY_WIDTH = 140.dp
private val TIME_COLUMN_WIDTH = 48.dp
private val START_HOUR = 8
private val END_HOUR = 19
private val DAY_NAMES = listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa")

// Rotating palette for courses
private val COURSE_COLORS = listOf(
    Color(0xFF1565C0), Color(0xFF00897B), Color(0xFF6A1B9A),
    Color(0xFFC62828), Color(0xFF2E7D32), Color(0xFFE65100),
    Color(0xFF283593), Color(0xFF00838F), Color(0xFF4E342E),
)

@Composable
fun WeekGrid(events: List<CalendarEvent>, weekStart: LocalDate) {
    val tz = TimeZone.currentSystemDefault()

    Column {
        // Day headers
        Row {
            Box(modifier = Modifier.width(TIME_COLUMN_WIDTH))
            Row(modifier = Modifier.horizontalScroll(rememberScrollState())) {
                for (i in 0..5) {
                    val date = weekStart.plus(i, DateTimeUnit.DAY)
                    Box(modifier = Modifier.width(DAY_WIDTH).padding(4.dp)) {
                        Text(
                            text = "${DAY_NAMES[i]} ${date.dayOfMonth}.${date.monthNumber}.",
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold,
                        )
                    }
                }
            }
        }

        // Grid body
        Row(modifier = Modifier.verticalScroll(rememberScrollState())) {
            // Time labels
            Column {
                for (hour in START_HOUR..END_HOUR) {
                    Box(modifier = Modifier.height(HOUR_HEIGHT).width(TIME_COLUMN_WIDTH)) {
                        Text(
                            text = "${hour}:00",
                            fontSize = 10.sp,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.padding(end = 4.dp),
                        )
                    }
                }
            }

            // Day columns
            Row(modifier = Modifier.horizontalScroll(rememberScrollState())) {
                for (dayOffset in 0..5) {
                    val date = weekStart.plus(dayOffset, DateTimeUnit.DAY)
                    val dayEvents = events.filter { event ->
                        val eventDate = event.startTime.toLocalDateTime(tz).date
                        eventDate == date
                    }

                    Box(
                        modifier = Modifier
                            .width(DAY_WIDTH)
                            .height(HOUR_HEIGHT * (END_HOUR - START_HOUR + 1))
                            .border(0.5.dp, Color.LightGray),
                    ) {
                        dayEvents.forEach { event ->
                            EventBlock(event, tz)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun EventBlock(event: CalendarEvent, tz: TimeZone) {
    val startLocal = event.startTime.toLocalDateTime(tz)
    val endLocal = event.endTime?.toLocalDateTime(tz) ?: startLocal

    val startMinutes = (startLocal.hour - START_HOUR) * 60 + startLocal.minute
    val durationMinutes = ((endLocal.hour * 60 + endLocal.minute) -
            (startLocal.hour * 60 + startLocal.minute)).coerceAtLeast(30)

    val topOffset = (startMinutes.toFloat() / 60f) * HOUR_HEIGHT.value
    val blockHeight = (durationMinutes.toFloat() / 60f) * HOUR_HEIGHT.value
    val color = eventColor(event)

    Box(
        modifier = Modifier
            .offset(x = 2.dp, y = Dp(topOffset))
            .width(DAY_WIDTH - 4.dp)
            .height(Dp(blockHeight))
            .clip(RoundedCornerShape(4.dp))
            .background(color.copy(alpha = 0.85f))
            .padding(4.dp),
    ) {
        Column {
            Text(
                text = event.title,
                fontSize = 10.sp,
                fontWeight = FontWeight.SemiBold,
                color = Color.White,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
            )
            event.location?.let {
                Text(
                    text = it,
                    fontSize = 9.sp,
                    color = Color.White.copy(alpha = 0.8f),
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
    }
}

private fun eventColor(event: CalendarEvent): Color {
    val type = event.eventType.lowercase()
    val title = event.title.lowercase()

    return when {
        type.contains("exam") || type.contains("klausur") ||
                title.contains("klausur") || title.contains("prüfung") -> EventExam
        type.contains("tutorium") || title.contains("tutorium") -> EventTutorium
        type.contains("selbststudium") || type.contains("frei") ||
                title.contains("selbststudium") -> EventSelfStudy
        else -> COURSE_COLORS[event.title.hashCode().mod(COURSE_COLORS.size).let {
            if (it < 0) it + COURSE_COLORS.size else it
        }]
    }
}
