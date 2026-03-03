from datetime import date, datetime

from pydantic import BaseModel


class ExerciseOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    entity_id: int
    question: str
    exercise_type: str
    difficulty: str
    bloom_level: int
    options_json: dict | None = None
    is_answered: bool
    is_correct: bool | None = None
    correct_answer: str | None = None
    explanation: str | None = None
    user_answer: str | None = None
    score: float | None = None
    created_at: datetime
    answered_at: datetime | None = None


class AnswerRequest(BaseModel):
    user_answer: str
    rating: int  # 1=Again, 2=Hard, 3=Good, 4=Easy


class ExerciseRequest(BaseModel):
    entity_id: int
    difficulty: str = "medium"
    bloom_level: int | None = None


class SessionRequest(BaseModel):
    count: int = 10


class StatsOut(BaseModel):
    total_entities: int
    mastered_entities: int
    average_mastery: float
    total_exercises: int
    answered_exercises: int
    correct_exercises: int
    accuracy: float


class StreakOut(BaseModel):
    model_config = {"from_attributes": True}

    current_streak: int
    longest_streak: int
    last_activity_date: date | None = None
    total_active_days: int
    multiplier: float = 1.0


class PriorityOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    entity_id: int
    composite_score: float
    deadline_urgency: float
    topic_relevance: float
    mastery_gap: float
    decay_amount: float
    bloom_gap: float
    is_blocked: bool
    block_reason: str | None = None
    calculated_at: datetime
