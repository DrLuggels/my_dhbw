package com.dhbw.app.domain.model

import kotlinx.datetime.Instant
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class Document(
    val id: Int,
    val title: String,
    val filename: String,
    val filetype: String,
    val filesize: Int,
    @SerialName("doc_category") val docCategory: String,
    @SerialName("processing_status") val processingStatus: String,
    @SerialName("metadata_json") val metadataJson: Map<String, String>? = null,
    @SerialName("created_at") val createdAt: Instant,
    @SerialName("updated_at") val updatedAt: Instant,
)
