package com.dhbw.app.ui.calendar

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import com.dhbw.app.domain.model.CalendarEvent
import com.dhbw.app.ui.theme.EventExam
import com.dhbw.app.ui.theme.EventSelfStudy
import com.dhbw.app.ui.theme.EventTutorium
import kotlinx.coroutines.delay
import kotlinx.datetime.Clock
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.toLocalDateTime

private val HOUR_HEIGHT = 52.dp
private val TIME_COL = 32.dp
private val START_HOUR = 8
private val END_HOUR = 19
private val DAY_NAMES = listOf("Mo", "Di", "Mi", "Do", "Fr", "Sa")
private val NOW_COLOR = Color(0xFFE53935)

private val COURSE_COLORS = listOf(
    Color(0xFF1565C0), Color(0xFF00897B), Color(0xFF6A1B9A),
    Color(0xFFC62828), Color(0xFF2E7D32), Color(0xFFE65100),
    Color(0xFF283593), Color(0xFF00838F), Color(0xFF4E342E),
)

@Composable
fun WeekGrid(events: List<CalendarEvent>, weekStart: LocalDate) {
    val tz = TimeZone.currentSystemDefault()
    var selectedEvent by remember { mutableStateOf<CalendarEvent?>(null) }
    var nowMinutes by remember { mutableStateOf(currentMinutesOfDay(tz)) }
    var today by remember { mutableStateOf(currentDate(tz)) }

    // Hide Saturday if no events on that day
    val saturday = weekStart.plus(5, DateTimeUnit.DAY)
    val hasSaturdayEvents = events.any { event ->
        event.startTime.toLocalDateTime(tz).date == saturday
    }
    val dayCount = if (hasSaturdayEvents) 6 else 5

    // Update current time every minute
    LaunchedEffect(Unit) {
        while (true) {
            delay(60_000)
            nowMinutes = currentMinutesOfDay(tz)
            today = currentDate(tz)
        }
    }

    // Detail dialog
    selectedEvent?.let { event ->
        EventDetailDialog(event, tz, onDismiss = { selectedEvent = null })
    }

    BoxWithConstraints(modifier = Modifier.fillMaxWidth()) {
        val dayWidth = (maxWidth - TIME_COL) / dayCount

        Column {
            // Day headers
            Row(modifier = Modifier.fillMaxWidth()) {
                Box(modifier = Modifier.width(TIME_COL))
                for (i in 0 until dayCount) {
                    val date = weekStart.plus(i, DateTimeUnit.DAY)
                    val isToday = date == today
                    Box(
                        modifier = Modifier.width(dayWidth).padding(vertical = 4.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        Text(
                            text = "${DAY_NAMES[i]}\n${date.dayOfMonth}.${date.monthNumber}.",
                            fontSize = 10.sp,
                            fontWeight = if (isToday) FontWeight.Bold else FontWeight.SemiBold,
                            textAlign = TextAlign.Center,
                            lineHeight = 13.sp,
                            color = if (isToday) MaterialTheme.colorScheme.primary
                            else MaterialTheme.colorScheme.onSurface,
                        )
                    }
                }
            }

            // Grid body
            Row(modifier = Modifier.fillMaxWidth().verticalScroll(rememberScrollState())) {
                // Time labels
                Column {
                    for (hour in START_HOUR..END_HOUR) {
                        Box(modifier = Modifier.height(HOUR_HEIGHT).width(TIME_COL)) {
                            Text(
                                text = "$hour",
                                fontSize = 9.sp,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(end = 2.dp),
                            )
                        }
                    }
                }

                // Day columns
                for (dayOffset in 0 until dayCount) {
                    val date = weekStart.plus(dayOffset, DateTimeUnit.DAY)
                    val isToday = date == today
                    val dayEvents = events.filter { event ->
                        event.startTime.toLocalDateTime(tz).date == date
                    }

                    Box(
                        modifier = Modifier
                            .width(dayWidth)
                            .height(HOUR_HEIGHT * (END_HOUR - START_HOUR + 1))
                            .border(0.5.dp, Color.LightGray),
                    ) {
                        dayEvents.forEach { event ->
                            EventBlock(event, tz, dayWidth, onClick = { selectedEvent = event })
                        }

                        // "Now" indicator line on today's column
                        if (isToday) {
                            val minSinceStart = nowMinutes - START_HOUR * 60
                            if (minSinceStart in 0..(END_HOUR - START_HOUR + 1) * 60) {
                                val yOff = (minSinceStart.toFloat() / 60f) * HOUR_HEIGHT.value
                                // Red dot
                                Box(
                                    modifier = Modifier
                                        .offset(x = (-3).dp, y = Dp(yOff - 3))
                                        .size(8.dp)
                                        .zIndex(10f)
                                        .clip(CircleShape)
                                        .background(NOW_COLOR),
                                )
                                // Red line
                                Box(
                                    modifier = Modifier
                                        .offset(y = Dp(yOff - 0.5f))
                                        .fillMaxWidth()
                                        .height(1.5.dp)
                                        .zIndex(10f)
                                        .background(NOW_COLOR),
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun EventBlock(
    event: CalendarEvent,
    tz: TimeZone,
    dayWidth: Dp,
    onClick: () -> Unit,
) {
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
            .offset(x = 1.dp, y = Dp(topOffset))
            .width(dayWidth - 2.dp)
            .height(Dp(blockHeight))
            .clip(RoundedCornerShape(3.dp))
            .background(color.copy(alpha = 0.85f))
            .clickable(onClick = onClick)
            .padding(2.dp),
    ) {
        Column {
            Text(
                text = event.title,
                fontSize = 8.sp,
                fontWeight = FontWeight.SemiBold,
                color = Color.White,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                lineHeight = 10.sp,
            )
            event.location?.let {
                Text(
                    text = it,
                    fontSize = 7.sp,
                    color = Color.White.copy(alpha = 0.8f),
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
    }
}

private fun currentMinutesOfDay(tz: TimeZone): Int {
    val now = Clock.System.now().toLocalDateTime(tz)
    return now.hour * 60 + now.minute
}

private fun currentDate(tz: TimeZone): LocalDate {
    return Clock.System.now().toLocalDateTime(tz).date
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
