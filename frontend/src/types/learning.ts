export type ExerciseType = 'multiple_choice' | 'fill_in_blank' | 'free_text'
export type Difficulty = 'easy' | 'medium' | 'hard'

export interface Exercise {
  id: number
  entity_id: number
  question: string
  exercise_type: ExerciseType
  difficulty: Difficulty
  bloom_level: number
  options_json: { options: string[] } | null
  is_answered: boolean
  is_correct: boolean | null
  correct_answer: string | null
  explanation: string | null
  user_answer: string | null
  score: number | null
  created_at: string
  answered_at: string | null
}

export interface AnswerRequest {
  user_answer: string
  rating: number
}

export interface LearningStats {
  total_entities: number
  mastered_entities: number
  average_mastery: number
  total_exercises: number
  answered_exercises: number
  correct_exercises: number
  accuracy: number
}

export interface Streak {
  current_streak: number
  longest_streak: number
  last_activity_date: string | null
  total_active_days: number
  multiplier: number
}

export interface LearningPriority {
  id: number
  entity_id: number
  composite_score: number
  deadline_urgency: number
  topic_relevance: number
  mastery_gap: number
  decay_amount: number
  bloom_gap: number
  is_blocked: boolean
  block_reason: string | null
  calculated_at: string
}
