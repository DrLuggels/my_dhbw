package com.dhbw.app.ui.calendar

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import cafe.adriel.voyager.koin.koinScreenModel
import com.dhbw.app.ui.components.ErrorScreen
import com.dhbw.app.ui.components.LoadingScreen
import com.dhbw.app.ui.theme.Accent
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.plus

@Composable
fun CalendarScreen() {
    val viewModel = koinScreenModel<CalendarViewModel>()
    val state by viewModel.state.collectAsState()

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

        // Week grid
        when {
            state.isLoading -> LoadingScreen()
            state.error != null -> ErrorScreen(state.error!!, onRetry = viewModel::loadWeek)
            else -> WeekGrid(
                events = state.events,
                weekStart = state.weekStart,
            )
        }
    }
}
