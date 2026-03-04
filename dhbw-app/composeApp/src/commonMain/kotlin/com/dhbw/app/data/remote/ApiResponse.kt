package com.dhbw.app.data.remote

import kotlinx.serialization.Serializable

@Serializable
data class ApiResponse<T>(
    val success: Boolean = true,
    val data: T? = null,
    val message: String = "",
    val errors: List<String> = emptyList(),
)
