"""Free Spaced Repetition Scheduler (FSRS) implementation.

Based on the FSRS algorithm with W0-W16 parameters.
Pure algorithm - no database dependencies.
"""

import math
from dataclasses import dataclass
from datetime import datetime, timedelta

# FSRS Parameters W0-W16
W = [
    0.4,   # W0: Initial stability for "Again"
    0.6,   # W1: Initial stability for "Hard"
    2.4,   # W2: Initial stability for "Good"
    5.8,   # W3: Initial stability for "Easy"
    4.93,  # W4: Stability multiplier
    0.94,  # W5: Difficulty factor
    0.86,  # W6: Difficulty factor
    0.01,  # W7: Difficulty factor
    1.49,  # W8: Difficulty factor
    0.14,  # W9: Recall factor
    0.94,  # W10: Recall factor
    2.18,  # W11: Recall factor
    0.05,  # W12: Forget factor
    0.34,  # W13: Forget factor
    1.26,  # W14: Forget factor
    0.29,  # W15: Forget factor
    2.61,  # W16: Forget factor
]

# States
STATE_NEW = 0
STATE_LEARNING = 1
STATE_REVIEW = 2
STATE_RELEARNING = 3

# Ratings
RATING_AGAIN = 1
RATING_HARD = 2
RATING_GOOD = 3
RATING_EASY = 4

TARGET_RETENTION = 0.9
MAX_INTERVAL = 365
MIN_INTERVAL = 1

# Decay constants (Ebbinghaus)
DECAY_RATE_DEFAULT = 0.05
DECAY_MINIMUM = 0.40


@dataclass
class FSRSState:
    """Current learning state of an item."""

    state: int = STATE_NEW
    stability: float = 0.0
    difficulty: float = 0.0
    last_review: datetime | None = None
    interval: int = 0


@dataclass
class ReviewResult:
    """Result after processing a review."""

    new_state: int
    stability: float
    difficulty: float
    interval: int
    next_review: datetime


def process_review(current: FSRSState, rating: int, now: datetime | None = None) -> ReviewResult:
    """Process a review and compute the next scheduling state.

    Args:
        current: Current FSRS state of the item.
        rating: User rating (1=Again, 2=Hard, 3=Good, 4=Easy).
        now: Current timestamp (defaults to utcnow).

    Returns:
        New state with next review date.
    """
    now = now or datetime.utcnow()

    if current.state == STATE_NEW:
        return _process_new(rating, now)

    elapsed = (now - current.last_review).days if current.last_review else 0
    retrievability = _retrievability(elapsed, current.stability)

    if rating == RATING_AGAIN:
        return _process_forget(current, retrievability, now)

    new_difficulty = _next_difficulty(current.difficulty, rating)
    new_stability = _next_stability(current.stability, new_difficulty, retrievability, rating)
    interval = _next_interval(new_stability)
    new_state = STATE_REVIEW

    return ReviewResult(
        new_state=new_state,
        stability=new_stability,
        difficulty=new_difficulty,
        interval=interval,
        next_review=now + timedelta(days=interval),
    )


def _process_new(rating: int, now: datetime) -> ReviewResult:
    """Handle first review of a new item."""
    stability = W[rating - 1]
    difficulty = _init_difficulty(rating)
    interval = max(MIN_INTERVAL, round(stability))

    state = STATE_LEARNING if rating <= RATING_GOOD else STATE_REVIEW

    return ReviewResult(
        new_state=state,
        stability=stability,
        difficulty=difficulty,
        interval=interval,
        next_review=now + timedelta(days=interval),
    )


def _process_forget(current: FSRSState, retrievability: float, now: datetime) -> ReviewResult:
    """Handle a failed review (Again rating)."""
    new_difficulty = _next_difficulty(current.difficulty, RATING_AGAIN)
    new_stability = W[12] * math.pow(current.stability, W[13]) * (
        math.exp(W[14] * (1 - retrievability)) - 1
    )
    new_stability = max(W[0], new_stability)

    return ReviewResult(
        new_state=STATE_RELEARNING,
        stability=new_stability,
        difficulty=new_difficulty,
        interval=MIN_INTERVAL,
        next_review=now + timedelta(days=MIN_INTERVAL),
    )


def _init_difficulty(rating: int) -> float:
    return max(1.0, min(10.0, W[5] - math.exp(W[6] * (rating - 3)) + 1))


def _next_difficulty(difficulty: float, rating: int) -> float:
    delta = -(W[7] * (rating - 3))
    new_d = difficulty + delta
    # Mean reversion
    new_d = W[5] + (new_d - W[5]) * W[8]
    return max(1.0, min(10.0, new_d))


def _next_stability(
    stability: float, difficulty: float, retrievability: float, rating: int
) -> float:
    modifier = 1.0
    if rating == RATING_HARD:
        modifier = W[9]
    elif rating == RATING_EASY:
        modifier = W[10]

    new_s = stability * (
        1 + math.exp(W[4]) * (11 - difficulty) * math.pow(stability, -W[11])
        * (math.exp(W[14] * (1 - retrievability)) - 1) * modifier
    )
    return max(0.1, new_s)


def _next_interval(stability: float) -> int:
    interval = stability * math.log(TARGET_RETENTION) / math.log(0.9)
    return max(MIN_INTERVAL, min(MAX_INTERVAL, round(interval)))


def _retrievability(elapsed_days: int, stability: float) -> float:
    if stability <= 0:
        return 0.0
    return math.exp(math.log(0.9) * elapsed_days / stability)


def compute_decay(days_since_interaction: int, decay_rate: float = DECAY_RATE_DEFAULT) -> float:
    """Compute Ebbinghaus exponential decay factor.

    Returns a value between DECAY_MINIMUM and 1.0.
    """
    factor = math.exp(-decay_rate * days_since_interaction)
    return DECAY_MINIMUM + (1.0 - DECAY_MINIMUM) * factor


def effective_mastery(
    fsrs_mastery: float,
    days_since: int,
    decay_rate: float = DECAY_RATE_DEFAULT,
) -> float:
    """Compute effective mastery including decay."""
    return fsrs_mastery * compute_decay(days_since, decay_rate)
