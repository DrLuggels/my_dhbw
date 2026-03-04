package com.dhbw.app.ui.learning

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.dhbw.app.ui.theme.ErrorRed
import com.dhbw.app.ui.theme.RatingAgain
import com.dhbw.app.ui.theme.RatingEasy
import com.dhbw.app.ui.theme.RatingGood
import com.dhbw.app.ui.theme.RatingHard
import com.dhbw.app.ui.theme.SuccessGreen

@Composable
fun AnswerResult(state: LearningState, viewModel: LearningViewModel) {
    val answered = state.answeredExercise ?: return
    val isCorrect = answered.isCorrect == true

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        // Correct/Incorrect banner
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = if (isCorrect)
                    SuccessGreen.copy(alpha = 0.1f)
                else
                    ErrorRed.copy(alpha = 0.1f),
            ),
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = if (isCorrect) "Richtig!" else "Falsch",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    color = if (isCorrect) SuccessGreen else ErrorRed,
                )
            }
        }

        // Correct answer
        answered.correctAnswer?.let { correct ->
            Card(modifier = Modifier.fillMaxWidth(), elevation = CardDefaults.cardElevation(1.dp)) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text("Richtige Antwort", style = MaterialTheme.typography.labelSmall)
                    Text(correct, fontWeight = FontWeight.Medium)
                }
            }
        }

        // Your answer
        Card(modifier = Modifier.fillMaxWidth(), elevation = CardDefaults.cardElevation(1.dp)) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text("Deine Antwort", style = MaterialTheme.typography.labelSmall)
                Text(answered.userAnswer ?: state.userAnswer)
            }
        }

        // Explanation
        answered.explanation?.let { explanation ->
            Card(modifier = Modifier.fillMaxWidth(), elevation = CardDefaults.cardElevation(1.dp)) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text("Erklärung", style = MaterialTheme.typography.labelSmall)
                    Text(explanation, style = MaterialTheme.typography.bodyMedium)
                }
            }
        }

        // FSRS Rating buttons
        Text(
            text = "Wie schwer war das?",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold,
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            RatingButton("Nochmal", RatingAgain, Modifier.weight(1f)) {
                viewModel.submitRating(1)
            }
            RatingButton("Schwer", RatingHard, Modifier.weight(1f)) {
                viewModel.submitRating(2)
            }
            RatingButton("Gut", RatingGood, Modifier.weight(1f)) {
                viewModel.submitRating(3)
            }
            RatingButton("Leicht", RatingEasy, Modifier.weight(1f)) {
                viewModel.submitRating(4)
            }
        }
    }
}

@Composable
private fun RatingButton(
    label: String,
    color: androidx.compose.ui.graphics.Color,
    modifier: Modifier = Modifier,
    onClick: () -> Unit,
) {
    Button(
        onClick = onClick,
        modifier = modifier,
        colors = ButtonDefaults.buttonColors(containerColor = color),
        shape = RoundedCornerShape(8.dp),
    ) {
        Text(label, style = MaterialTheme.typography.labelMedium)
    }
}
