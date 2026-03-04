package com.dhbw.app.di

import com.dhbw.app.data.remote.CalendarApi
import com.dhbw.app.data.remote.DocumentApi
import com.dhbw.app.data.remote.LearningApi
import com.dhbw.app.data.remote.createHttpClient
import com.dhbw.app.data.repository.CalendarRepository
import com.dhbw.app.data.repository.LearningRepository
import com.dhbw.app.ui.calendar.CalendarViewModel
import com.dhbw.app.ui.camera.CameraViewModel
import com.dhbw.app.ui.dashboard.DashboardViewModel
import com.dhbw.app.ui.learning.LearningViewModel
import org.koin.core.module.dsl.factoryOf
import org.koin.dsl.module

const val DEFAULT_BASE_URL = "https://localhost"

val appModule = module {
    // HTTP Client
    single { createHttpClient(getProperty("BASE_URL", DEFAULT_BASE_URL)) }

    // API clients
    single { LearningApi(get()) }
    single { CalendarApi(get()) }
    single { DocumentApi(get()) }

    // Repositories
    single { LearningRepository(get()) }
    single { CalendarRepository(get()) }

    // ViewModels
    factoryOf(::DashboardViewModel)
    factoryOf(::LearningViewModel)
    factoryOf(::CalendarViewModel)
    factoryOf(::CameraViewModel)
}
