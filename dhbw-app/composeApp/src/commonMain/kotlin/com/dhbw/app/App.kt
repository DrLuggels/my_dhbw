package com.dhbw.app

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import cafe.adriel.voyager.navigator.tab.CurrentTab
import cafe.adriel.voyager.navigator.tab.LocalTabNavigator
import cafe.adriel.voyager.navigator.tab.Tab
import cafe.adriel.voyager.navigator.tab.TabNavigator
import com.dhbw.app.ui.components.OfflineIndicator
import com.dhbw.app.ui.navigation.CalendarTab
import com.dhbw.app.ui.navigation.CameraTab
import com.dhbw.app.ui.navigation.DashboardTab
import com.dhbw.app.ui.navigation.LearningTab
import com.dhbw.app.ui.theme.DhbwTheme

@Composable
fun App() {
    var isOffline by remember { mutableStateOf(false) }

    DhbwTheme {
        TabNavigator(DashboardTab) {
            Scaffold(
                bottomBar = {
                    NavigationBar {
                        TabNavItem(DashboardTab)
                        TabNavItem(LearningTab)
                        TabNavItem(CalendarTab)
                        TabNavItem(CameraTab)
                    }
                },
            ) { padding ->
                Column(modifier = Modifier.fillMaxSize()) {
                    OfflineIndicator(isOffline)
                    Box(modifier = Modifier.fillMaxSize().padding(padding)) {
                        CurrentTab()
                    }
                }
            }
        }
    }
}

@Composable
private fun RowScope.TabNavItem(tab: Tab) {
    val tabNavigator = LocalTabNavigator.current
    NavigationBarItem(
        selected = tabNavigator.current == tab,
        onClick = { tabNavigator.current = tab },
        icon = {
            tab.options.icon?.let { Icon(it, contentDescription = tab.options.title) }
        },
        label = { Text(tab.options.title) },
    )
}
