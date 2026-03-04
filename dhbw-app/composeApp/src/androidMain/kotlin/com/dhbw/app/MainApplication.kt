package com.dhbw.app

import android.app.Application
import com.dhbw.app.di.appModule
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin

class MainApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        startKoin {
            androidContext(this@MainApplication)
            properties(mapOf("BASE_URL" to "http://192.168.178.198:8090"))
            modules(appModule)
        }
    }
}
