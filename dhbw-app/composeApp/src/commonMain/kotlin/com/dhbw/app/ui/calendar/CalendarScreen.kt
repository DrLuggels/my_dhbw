package com.dhbw.app.ui.calendar

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.dhbw.app.ui.components.ErrorScreen
import com.dhbw.app.ui.components.LoadingScreen
import com.dhbw.app.ui.theme.Accent
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.plus
import org.koin.compose.koinInject

@Composable
fun CalendarScreen() {
    val viewModel: CalendarViewModel = koinInject()
    val state: CalendarState by viewModel.state.collectAsState()

    Column(
        modifier = Modifier.fillMaxSize().padding(8.dp),
    ) {
        // Header with week navigation
        Row(
            modifier = Modifier.fillMaxWidth().padding(8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            TextButton(onClick = viewModel::previousWeek) {
                Text("<")
            }

            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = "Stundenplan",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    text = "${state.weekStart} — ${state.weekStart.plus(5, DateTimeUnit.DAY)}",
                    style = MaterialTheme.typography.bodySmall,
                )
            }

            TextButton(onClick = viewModel::nextWeek) {
                Text(">")
            }
        }

        // Today button + Sync
        Row(
            modifier = Modifier.fillMaxWidth().padding(horizontal = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            TextButton(onClick = viewModel::goToToday) {
                Text("Heute")
            }
            Button(
                onClick = viewModel::syncRapla,
                enabled = !state.isSyncing,
                colors = ButtonDefaults.buttonColors(containerColor = Accent),
            ) {
                if (state.isSyncing) {
                    CircularProgressIndicator(
                        modifier = Modifier.padding(end = 8.dp),
                        strokeWidth = 2.dp,
                    )
                }
                Text("Rapla Sync")
            }
        }

        // Week grid with swipe + slide animation
        when {
            state.isLoading && state.events.isEmpty() -> LoadingScreen()
            state.error != null -> ErrorScreen(state.error!!, onRetry = viewModel::loadWeek)
            else -> {
                var totalDrag by remember { mutableStateOf(0f) }
                Box(
                    modifier = Modifier
                        .weight(1f)
                        .pointerInput(state.weekStart) {
                            detectHorizontalDragGestures(
                                onDragStart = { totalDrag = 0f },
                                onDragEnd = {
                                    if (totalDrag > 80) viewModel.previousWeek()
                                    else if (totalDrag < -80) viewModel.nextWeek()
                                },
                                onHorizontalDrag = { _, dragAmount ->
                                    totalDrag += dragAmount
                                },
                            )
                        },
                ) {
                    val spec = tween<androidx.compose.ui.unit.IntOffset>(
                        durationMillis = 300,
                        easing = FastOutSlowInEasing,
                    )
                    AnimatedContent(
                        targetState = state.weekStart,
                        transitionSpec = {
                            if (targetState > initialState) {
                                slideInHorizontally(spec) { it } togetherWith
                                    slideOutHorizontally(spec) { -it }
                            } else {
                                slideInHorizontally(spec) { -it } togetherWith
                                    slideOutHorizontally(spec) { it }
                            }
                        },
                        label = "weekSlide",
                    ) { weekStart ->
                        WeekGrid(
                            events = state.events,
                            weekStart = weekStart,
                        )
                    }
                }
            }
        }
    }
}
