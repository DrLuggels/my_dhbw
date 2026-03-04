package com.dhbw.app.ui.learning

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import cafe.adriel.voyager.koin.koinScreenModel
import com.dhbw.app.ui.components.ErrorScreen
import com.dhbw.app.ui.components.LoadingScreen
import com.dhbw.app.ui.theme.Accent
import com.dhbw.app.ui.theme.MasteryHigh
import com.dhbw.app.ui.theme.MasteryLow
import com.dhbw.app.ui.theme.MasteryMedium
import com.dhbw.app.ui.theme.Primary

@Composable
fun LearningScreen() {
    val viewModel = koinScreenModel<LearningViewModel>()
    val state by viewModel.state.collectAsState()

    when {
        state.isLoading -> LoadingScreen()
        state.error != null -> ErrorScreen(state.error!!, onRetry = viewModel::loadOverview)
        else -> when (state.phase) {
            LearningPhase.OVERVIEW -> LearningOverview(state, viewModel)
            LearningPhase.EXERCISE -> ExercisePlayer(state, viewModel)
            LearningPhase.RESULT -> AnswerResult(state, viewModel)
        }
    }
}

@Composable
private fun LearningOverview(state: LearningState, viewModel: LearningViewModel) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text(
            text = "Lernen",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold,
        )

        // Stats cards
        state.stats?.let { stats ->
            StatCard("Mastery", "${(stats.averageMastery * 100).toInt()}%",
                masteryColor((stats.averageMastery * 100)))
            StatCard("Genauigkeit", "${(stats.accuracy * 100).toInt()}%", Accent)
            StatCard("Beantwortet", "${stats.answeredExercises} / ${stats.totalExercises}", Primary)
        }

        state.streak?.let { streak ->
            StatCard("Streak", "${streak.currentStreak} Tage (×${streak.multiplier})", Primary)
        }

        if (state.dueCount > 0) {
            StatCard("Fällige Übungen", "${state.dueCount}", MasteryLow)
        }

        Button(
            onClick = viewModel::startLearning,
            modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Primary),
        ) {
            Text(
                "Lernen starten",
                modifier = Modifier.padding(vertical = 4.dp),
                fontSize = 16.sp,
            )
        }
    }
}

@Composable
private fun StatCard(
    title: String,
    value: String,
    color: androidx.compose.ui.graphics.Color,
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        elevation = CardDefaults.cardElevation(1.dp),
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(title, style = MaterialTheme.typography.bodySmall)
            Text(value, fontSize = 22.sp, fontWeight = FontWeight.Bold, color = color)
        }
    }
}

private fun masteryColor(percent: Double) = when {
    percent < 40 -> MasteryLow
    percent < 70 -> MasteryMedium
    else -> MasteryHigh
}
