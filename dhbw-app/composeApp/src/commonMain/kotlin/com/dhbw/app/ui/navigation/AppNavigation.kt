package com.dhbw.app.ui.navigation

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Star
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.graphics.vector.rememberVectorPainter
import cafe.adriel.voyager.navigator.tab.Tab
import cafe.adriel.voyager.navigator.tab.TabOptions
import com.dhbw.app.ui.calendar.CalendarScreen
import com.dhbw.app.ui.camera.CameraScreen
import com.dhbw.app.ui.dashboard.DashboardScreen
import com.dhbw.app.ui.learning.LearningScreen

object DashboardTab : Tab {
    override val options: TabOptions
        @Composable get() {
            val icon = rememberVectorPainter(Icons.Default.Home)
            return remember { TabOptions(index = 0u, title = "Dashboard", icon = icon) }
        }

    @Composable
    override fun Content() = DashboardScreen()
}

object LearningTab : Tab {
    override val options: TabOptions
        @Composable get() {
            val icon = rememberVectorPainter(Icons.Default.Star)
            return remember { TabOptions(index = 1u, title = "Lernen", icon = icon) }
        }

    @Composable
    override fun Content() = LearningScreen()
}

object CalendarTab : Tab {
    override val options: TabOptions
        @Composable get() {
            val icon = rememberVectorPainter(Icons.Default.DateRange)
            return remember { TabOptions(index = 2u, title = "Stundenplan", icon = icon) }
        }

    @Composable
    override fun Content() = CalendarScreen()
}

object CameraTab : Tab {
    override val options: TabOptions
        @Composable get() {
            val icon = rememberVectorPainter(Icons.Default.List)
            return remember { TabOptions(index = 3u, title = "Kamera", icon = icon) }
        }

    @Composable
    override fun Content() = CameraScreen()
}
