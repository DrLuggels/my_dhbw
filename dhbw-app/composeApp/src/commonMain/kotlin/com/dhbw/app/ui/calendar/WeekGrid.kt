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
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import com.dhbw.app.domain.model.CalendarEvent
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

// Exact 15-color palette from web frontend
private val PALETTE = listOf(
    Color(0xFF1565C0), Color(0xFF2E7D32), Color(0xFF6A1B9A),
    Color(0xFFE65100), Color(0xFF00838F), Color(0xFFAD1457),
    Color(0xFF283593), Color(0xFF4E342E), Color(0xFF00695C),
    Color(0xFFBF360C), Color(0xFF1B5E20), Color(0xFF4A148C),
    Color(0xFF0D47A1), Color(0xFF880E4F), Color(0xFF33691E),
)

private val EXAM_COLOR = Color(0xFFC62828)
private val TUTORIUM_COLOR = Color(0xFF546E7A)
private val SELF_STUDY_COLOR = Color(0xFF78909C)

private val HOLIDAY_KEYWORDS = listOf(
    "selbststudium", "feiertag", "rosenmontag", "karfreitag",
    "ostermontag", "pfingst", "fronleichnam", "himmelfahrt",
    "tag der arbeit", "ostersamstag",
)

@Composable
fun WeekGrid(events: List<CalendarEvent>, weekStart: LocalDate) {
    val tz = TimeZone.currentSystemDefault()
    var selectedEvent by remember { mutableStateOf<CalendarEvent?>(null) }
    var nowMinutes by remember { mutableStateOf(currentMinutesOfDay(tz)) }
    var today by remember { mutableStateOf(currentDate(tz)) }

    // Build sequential color map (same logic as web frontend)
    val colorMap = remember(events) { buildCourseColorMap(events) }

    // Hide Saturday if no events
    val saturday = weekStart.plus(5, DateTimeUnit.DAY)
    val hasSaturdayEvents = events.any { it.startTime.toLocalDateTime(tz).date == saturday }
    val dayCount = if (hasSaturdayEvents) 6 else 5

    LaunchedEffect(Unit) {
        while (true) {
            delay(60_000)
            nowMinutes = currentMinutesOfDay(tz)
            today = currentDate(tz)
        }
    }

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

                for (dayOffset in 0 until dayCount) {
                    val date = weekStart.plus(dayOffset, DateTimeUnit.DAY)
                    val isToday = date == today
                    val dayEvents = events.filter {
                        it.startTime.toLocalDateTime(tz).date == date
                    }

                    Box(
                        modifier = Modifier
                            .width(dayWidth)
                            .height(HOUR_HEIGHT * (END_HOUR - START_HOUR + 1))
                            .border(0.5.dp, Color.LightGray),
                    ) {
                        dayEvents.forEach { event ->
                            EventBlock(
                                event, tz, dayWidth, colorMap,
                                onClick = { selectedEvent = event },
                            )
                        }

                        if (isToday) {
                            val minSinceStart = nowMinutes - START_HOUR * 60
                            if (minSinceStart in 0..(END_HOUR - START_HOUR + 1) * 60) {
                                val yOff = (minSinceStart.toFloat() / 60f) * HOUR_HEIGHT.value
                                Box(
                                    modifier = Modifier
                                        .offset(x = (-3).dp, y = Dp(yOff - 3))
                                        .size(8.dp)
                                        .zIndex(10f)
                                        .clip(CircleShape)
                                        .background(NOW_COLOR),
                                )
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
    colorMap: Map<String, Color>,
    onClick: () -> Unit,
) {
    val startLocal = event.startTime.toLocalDateTime(tz)
    val endLocal = event.endTime?.toLocalDateTime(tz) ?: startLocal

    val startMinutes = (startLocal.hour - START_HOUR) * 60 + startLocal.minute
    val durationMinutes = ((endLocal.hour * 60 + endLocal.minute) -
        (startLocal.hour * 60 + startLocal.minute)).coerceAtLeast(30)

    val topOffset = (startMinutes.toFloat() / 60f) * HOUR_HEIGHT.value
    val blockHeight = (durationMinutes.toFloat() / 60f) * HOUR_HEIGHT.value
    val color = eventColor(event, colorMap)
    val isExam = isExam(event)

    Box(
        modifier = Modifier
            .offset(x = 1.dp, y = Dp(topOffset))
            .width(dayWidth - 2.dp)
            .height(Dp(blockHeight))
            .clip(RoundedCornerShape(4.dp))
            .then(if (isExam) Modifier.shadow(4.dp, RoundedCornerShape(4.dp)) else Modifier)
            .background(color)
            .clickable(onClick = onClick)
            .padding(start = 4.dp, end = 2.dp, top = 2.dp, bottom = 2.dp),
    ) {
        Column {
            Text(
                text = if (isExam) "\u26A0 ${event.title}" else event.title,
                fontSize = 8.sp,
                fontWeight = FontWeight.SemiBold,
                color = Color.White,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                lineHeight = 10.sp,
            )
            event.location?.let {
                Text(
                    text = shortRoom(it),
                    fontSize = 7.sp,
                    color = Color.White.copy(alpha = 0.9f),
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
    }
}

/** Extract course key from parentheses or normalize title — matches web frontend */
private fun getCourseKey(title: String): String {
    val match = Regex("\\(([^)]+)\\)").find(title)
    if (match != null) return match.groupValues[1].trim()
    return title.replace(Regex("\\s+"), " ").trim().lowercase()
}

/** Build sequential color map like web frontend (not hash-based) */
private fun buildCourseColorMap(events: List<CalendarEvent>): Map<String, Color> {
    val map = mutableMapOf<String, Color>()
    var idx = 0
    for (event in events) {
        if (isExam(event) || isTutorium(event) || isSelfStudy(event)) continue
        val key = getCourseKey(event.title)
        if (key !in map) {
            map[key] = PALETTE[idx % PALETTE.size]
            idx++
        }
    }
    return map
}

private fun eventColor(event: CalendarEvent, colorMap: Map<String, Color>): Color {
    if (isExam(event)) return EXAM_COLOR
    if (isTutorium(event)) return TUTORIUM_COLOR
    if (isSelfStudy(event)) return SELF_STUDY_COLOR
    return colorMap[getCourseKey(event.title)] ?: PALETTE[0]
}

private fun isExam(event: CalendarEvent): Boolean {
    val t = event.title.lowercase()
    return t.contains("klausur") || t.contains("kurztest") ||
        t.contains("prüfung") || t.contains("exam")
}

private fun isTutorium(event: CalendarEvent): Boolean {
    return event.title.lowercase().contains("tutorium")
}

private fun isSelfStudy(event: CalendarEvent): Boolean {
    val t = event.title.lowercase()
    return HOLIDAY_KEYWORDS.any { t.contains(it) }
}

/** Truncate room at first parenthesis — matches web frontend */
private fun shortRoom(loc: String): String {
    val idx = loc.indexOf('(')
    return if (idx > 0) loc.substring(0, idx).trim() else loc
}

private fun currentMinutesOfDay(tz: TimeZone): Int {
    val now = Clock.System.now().toLocalDateTime(tz)
    return now.hour * 60 + now.minute
}

private fun currentDate(tz: TimeZone): LocalDate {
    return Clock.System.now().toLocalDateTime(tz).date
}
