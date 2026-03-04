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
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.dhbw.app.domain.model.ExerciseType
import com.dhbw.app.ui.theme.Primary

@Composable
fun ExercisePlayer(state: LearningState, viewModel: LearningViewModel) {
    val exercise = state.exercise ?: return

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        // Header
        TextButton(onClick = viewModel::backToOverview) {
            Text("< Zurück")
        }

        // Question card
        Card(
            modifier = Modifier.fillMaxWidth(),
            elevation = CardDefaults.cardElevation(1.dp),
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = "Bloom Level ${exercise.bloomLevel} • ${exercise.difficulty}",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Text(
                    text = exercise.question,
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.Medium,
                    modifier = Modifier.padding(top = 8.dp),
                )
            }
        }

        // Answer input based on type
        when (exercise.exerciseType) {
            ExerciseType.MULTIPLE_CHOICE -> MultipleChoice(
                options = exercise.optionsJson?.options ?: emptyList(),
                selected = state.userAnswer,
                onSelect = viewModel::updateAnswer,
            )
            ExerciseType.FILL_IN_BLANK -> FillInBlank(
                value = state.userAnswer,
                onValueChange = viewModel::updateAnswer,
            )
            ExerciseType.FREE_TEXT -> FreeText(
                value = state.userAnswer,
                onValueChange = viewModel::updateAnswer,
            )
        }

        // Submit button
        Button(
            onClick = viewModel::submitAnswer,
            modifier = Modifier.fillMaxWidth(),
            enabled = state.userAnswer.isNotBlank(),
            colors = ButtonDefaults.buttonColors(containerColor = Primary),
        ) {
            Text("Antwort absenden", modifier = Modifier.padding(vertical = 4.dp))
        }
    }
}
