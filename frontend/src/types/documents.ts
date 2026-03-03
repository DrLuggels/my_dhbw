export type ProcessingStatus = 'pending' | 'processing' | 'done' | 'error'
export type DocCategory = 'slides_export' | 'textbook' | 'exercise_sheet' | 'paper' | 'scan' | 'unknown'

export interface Document {
  id: number
  title: string
  filename: string
  filetype: string
  filesize: number
  doc_category: DocCategory
  processing_status: ProcessingStatus
  metadata_json: Record<string, unknown> | null
  created_at: string
  updated_at: string
}

export interface Chunk {
  id: number
  document_id: number
  content: string
  chunk_index: number
  chunk_type: string
  topic_label: string | null
  section_heading: string | null
  page_number: number | null
  metadata_json: Record<string, unknown> | null
  created_at: string
}

export interface DocumentDetail extends Document {
  chunks: Chunk[]
  chunk_count: number
}
