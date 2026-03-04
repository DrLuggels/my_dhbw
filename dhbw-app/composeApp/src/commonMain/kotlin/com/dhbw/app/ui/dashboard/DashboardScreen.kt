package com.dhbw.app.ui.dashboard

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
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
import org.koin.compose.koinInject
import cafe.adriel.voyager.navigator.tab.LocalTabNavigator
import com.dhbw.app.ui.components.ErrorScreen
import com.dhbw.app.ui.components.LoadingScreen
import com.dhbw.app.ui.navigation.CameraTab
import com.dhbw.app.ui.navigation.LearningTab
import com.dhbw.app.ui.theme.Accent
import com.dhbw.app.ui.theme.MasteryHigh
import com.dhbw.app.ui.theme.MasteryLow
import com.dhbw.app.ui.theme.MasteryMedium
import com.dhbw.app.ui.theme.Primary

@Composable
fun DashboardScreen() {
    val viewModel: DashboardViewModel = koinInject()
    val state: DashboardState by viewModel.state.collectAsState()

    when {
        state.isLoading -> LoadingScreen()
        state.error != null -> ErrorScreen(state.error!!, onRetry = viewModel::loadDashboard)
        else -> DashboardContent(state)
    }
}

@Composable
private fun DashboardContent(state: DashboardState) {
    val tabNavigator = LocalTabNavigator.current

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text(
            text = "Dashboard",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold,
        )

        // KPI Cards
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            KpiCard(
                title = "Mastery",
                value = "${((state.stats?.averageMastery ?: 0.0) * 100).toInt()}%",
                color = masteryColor((state.stats?.averageMastery ?: 0.0) * 100),
                modifier = Modifier.weight(1f),
            )
            KpiCard(
                title = "Streak",
                value = "${state.streak?.currentStreak ?: 0}",
                color = Primary,
                modifier = Modifier.weight(1f),
            )
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            KpiCard(
                title = "Genauigkeit",
                value = "${((state.stats?.accuracy ?: 0.0) * 100).toInt()}%",
                color = Accent,
                modifier = Modifier.weight(1f),
            )
            KpiCard(
                title = "Fällig",
                value = "${state.dueCount}",
                color = if (state.dueCount > 0) MasteryLow else MasteryHigh,
                modifier = Modifier.weight(1f),
            )
        }

        // Quick Actions
        Text(
            text = "Quick Actions",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(top = 8.dp),
        )

        Button(
            onClick = { tabNavigator.current = LearningTab },
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = Primary),
        ) {
            Text("Lernen starten", modifier = Modifier.padding(vertical = 4.dp))
        }

        Button(
            onClick = { tabNavigator.current = CameraTab },
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = Accent),
        ) {
            Text("Foto aufnehmen", modifier = Modifier.padding(vertical = 4.dp))
        }
    }
}

@Composable
private fun KpiCard(
    title: String,
    value: String,
    color: androidx.compose.ui.graphics.Color,
    modifier: Modifier = Modifier,
) {
    Card(
        modifier = modifier,
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Text(
                text = value,
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold,
                color = color,
            )
            Text(
                text = title,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

private fun masteryColor(percent: Double) = when {
    percent < 40 -> MasteryLow
    percent < 70 -> MasteryMedium
    else -> MasteryHigh
}
