"""initial

Revision ID: 9f148d9a1bad
Revises:
Create Date: 2026-03-03 22:07:11.722360

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa
from sqlalchemy.dialects import postgresql
import pgvector.sqlalchemy.vector

# revision identifiers, used by Alembic.
revision: str = '9f148d9a1bad'
down_revision: Union[str, None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.execute('CREATE EXTENSION IF NOT EXISTS vector')
    op.create_table('calendar_events',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('title', sa.String(length=500), nullable=False),
    sa.Column('description', sa.Text(), nullable=True),
    sa.Column('start_time', sa.DateTime(timezone=True), nullable=False),
    sa.Column('end_time', sa.DateTime(timezone=True), nullable=True),
    sa.Column('all_day', sa.Boolean(), nullable=False),
    sa.Column('event_type', sa.String(length=50), nullable=False),
    sa.Column('source', sa.String(length=50), nullable=False),
    sa.Column('external_id', sa.String(length=200), nullable=True),
    sa.Column('subject', sa.String(length=200), nullable=True),
    sa.Column('location', sa.String(length=500), nullable=True),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('documents',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('title', sa.String(length=500), nullable=False),
    sa.Column('filename', sa.String(length=500), nullable=False),
    sa.Column('filepath', sa.String(length=1000), nullable=False),
    sa.Column('filetype', sa.String(length=50), nullable=False),
    sa.Column('filesize', sa.Integer(), nullable=False),
    sa.Column('doc_category', sa.String(length=50), nullable=False),
    sa.Column('processing_status', sa.String(length=50), nullable=False),
    sa.Column('metadata_json', postgresql.JSONB(astext_type=sa.Text()), nullable=True),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('learning_streak',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('current_streak', sa.Integer(), nullable=False),
    sa.Column('longest_streak', sa.Integer(), nullable=False),
    sa.Column('last_activity_date', sa.Date(), nullable=True),
    sa.Column('total_active_days', sa.Integer(), nullable=False),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('moodle_courses',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('moodle_id', sa.Integer(), nullable=False),
    sa.Column('shortname', sa.String(length=100), nullable=False),
    sa.Column('fullname', sa.String(length=500), nullable=False),
    sa.Column('summary', sa.Text(), nullable=True),
    sa.Column('start_date', sa.DateTime(timezone=True), nullable=True),
    sa.Column('end_date', sa.DateTime(timezone=True), nullable=True),
    sa.Column('last_synced', sa.DateTime(timezone=True), nullable=True),
    sa.PrimaryKeyConstraint('id'),
    sa.UniqueConstraint('moodle_id')
    )
    op.create_table('chunks',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('document_id', sa.Integer(), nullable=False),
    sa.Column('content', sa.Text(), nullable=False),
    sa.Column('chunk_index', sa.Integer(), nullable=False),
    sa.Column('chunk_type', sa.String(length=50), nullable=False),
    sa.Column('topic_label', sa.String(length=200), nullable=True),
    sa.Column('section_heading', sa.String(length=500), nullable=True),
    sa.Column('page_number', sa.Integer(), nullable=True),
    sa.Column('metadata_json', postgresql.JSONB(astext_type=sa.Text()), nullable=True),
    sa.Column('embedding', pgvector.sqlalchemy.vector.VECTOR(dim=1536), nullable=True),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.ForeignKeyConstraint(['document_id'], ['documents.id'], ondelete='CASCADE'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('moodle_assignments',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('course_id', sa.Integer(), nullable=False),
    sa.Column('moodle_id', sa.Integer(), nullable=False),
    sa.Column('name', sa.String(length=500), nullable=False),
    sa.Column('description', sa.Text(), nullable=True),
    sa.Column('due_date', sa.DateTime(timezone=True), nullable=True),
    sa.Column('status', sa.String(length=50), nullable=False),
    sa.ForeignKeyConstraint(['course_id'], ['moodle_courses.id'], ondelete='CASCADE'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('moodle_resources',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('course_id', sa.Integer(), nullable=False),
    sa.Column('moodle_id', sa.Integer(), nullable=False),
    sa.Column('name', sa.String(length=500), nullable=False),
    sa.Column('resource_type', sa.String(length=50), nullable=False),
    sa.Column('url', sa.String(length=1000), nullable=True),
    sa.Column('file_size', sa.Integer(), nullable=True),
    sa.Column('is_downloaded', sa.Boolean(), nullable=False),
    sa.Column('document_id', sa.Integer(), nullable=True),
    sa.Column('last_modified', sa.DateTime(timezone=True), nullable=True),
    sa.ForeignKeyConstraint(['course_id'], ['moodle_courses.id'], ondelete='CASCADE'),
    sa.ForeignKeyConstraint(['document_id'], ['documents.id'], ondelete='SET NULL'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('entities',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('name', sa.String(length=500), nullable=False),
    sa.Column('description', sa.Text(), nullable=True),
    sa.Column('entity_type', sa.String(length=50), nullable=False),
    sa.Column('subject', sa.String(length=200), nullable=True),
    sa.Column('topic', sa.String(length=200), nullable=True),
    sa.Column('subtopic', sa.String(length=200), nullable=True),
    sa.Column('importance', sa.Float(), nullable=False),
    sa.Column('confidence', sa.Float(), nullable=False),
    sa.Column('source_document_id', sa.Integer(), nullable=True),
    sa.Column('source_chunk_id', sa.Integer(), nullable=True),
    sa.Column('mastery_score', sa.Float(), nullable=False),
    sa.Column('bloom_level', sa.Integer(), nullable=False),
    sa.Column('next_review', sa.DateTime(timezone=True), nullable=True),
    sa.Column('fsrs_state', sa.Integer(), nullable=False),
    sa.Column('fsrs_stability', sa.Float(), nullable=False),
    sa.Column('fsrs_difficulty', sa.Float(), nullable=False),
    sa.Column('total_attempts', sa.Integer(), nullable=False),
    sa.Column('correct_attempts', sa.Integer(), nullable=False),
    sa.Column('easy_total', sa.Integer(), nullable=False),
    sa.Column('easy_correct', sa.Integer(), nullable=False),
    sa.Column('medium_total', sa.Integer(), nullable=False),
    sa.Column('medium_correct', sa.Integer(), nullable=False),
    sa.Column('hard_total', sa.Integer(), nullable=False),
    sa.Column('hard_correct', sa.Integer(), nullable=False),
    sa.Column('last_interaction', sa.DateTime(timezone=True), nullable=True),
    sa.Column('decay_rate', sa.Float(), nullable=False),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.ForeignKeyConstraint(['source_chunk_id'], ['chunks.id'], ondelete='SET NULL'),
    sa.ForeignKeyConstraint(['source_document_id'], ['documents.id'], ondelete='SET NULL'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('exercises',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('entity_id', sa.Integer(), nullable=False),
    sa.Column('question', sa.Text(), nullable=False),
    sa.Column('correct_answer', sa.Text(), nullable=False),
    sa.Column('explanation', sa.Text(), nullable=True),
    sa.Column('exercise_type', sa.String(length=50), nullable=False),
    sa.Column('difficulty', sa.String(length=20), nullable=False),
    sa.Column('bloom_level', sa.Integer(), nullable=False),
    sa.Column('options_json', postgresql.JSONB(astext_type=sa.Text()), nullable=True),
    sa.Column('is_answered', sa.Boolean(), nullable=False),
    sa.Column('is_correct', sa.Boolean(), nullable=True),
    sa.Column('user_answer', sa.Text(), nullable=True),
    sa.Column('score', sa.Float(), nullable=True),
    sa.Column('next_review', sa.DateTime(timezone=True), nullable=True),
    sa.Column('fsrs_state', sa.Integer(), nullable=False),
    sa.Column('source_chunk_id', sa.Integer(), nullable=True),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.Column('answered_at', sa.DateTime(timezone=True), nullable=True),
    sa.ForeignKeyConstraint(['entity_id'], ['entities.id'], ondelete='CASCADE'),
    sa.ForeignKeyConstraint(['source_chunk_id'], ['chunks.id'], ondelete='SET NULL'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('learning_priorities',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('entity_id', sa.Integer(), nullable=False),
    sa.Column('composite_score', sa.Float(), nullable=False),
    sa.Column('deadline_urgency', sa.Float(), nullable=False),
    sa.Column('topic_relevance', sa.Float(), nullable=False),
    sa.Column('mastery_gap', sa.Float(), nullable=False),
    sa.Column('decay_amount', sa.Float(), nullable=False),
    sa.Column('bloom_gap', sa.Float(), nullable=False),
    sa.Column('is_blocked', sa.Boolean(), nullable=False),
    sa.Column('block_reason', sa.String(length=500), nullable=True),
    sa.Column('calculated_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.ForeignKeyConstraint(['entity_id'], ['entities.id'], ondelete='CASCADE'),
    sa.PrimaryKeyConstraint('id')
    )
    op.create_table('relationships',
    sa.Column('id', sa.Integer(), nullable=False),
    sa.Column('source_entity_id', sa.Integer(), nullable=False),
    sa.Column('target_entity_id', sa.Integer(), nullable=False),
    sa.Column('relationship_type', sa.String(length=50), nullable=False),
    sa.Column('strength', sa.Float(), nullable=False),
    sa.Column('evidence', sa.Text(), nullable=True),
    sa.Column('confidence', sa.Float(), nullable=False),
    sa.Column('is_prerequisite', sa.Boolean(), nullable=False),
    sa.Column('prerequisite_strictness', sa.String(length=10), nullable=True),
    sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
    sa.ForeignKeyConstraint(['source_entity_id'], ['entities.id'], ondelete='CASCADE'),
    sa.ForeignKeyConstraint(['target_entity_id'], ['entities.id'], ondelete='CASCADE'),
    sa.PrimaryKeyConstraint('id')
    )


def downgrade() -> None:
    op.drop_table('relationships')
    op.drop_table('learning_priorities')
    op.drop_table('exercises')
    op.drop_table('entities')
    op.drop_table('moodle_resources')
    op.drop_table('moodle_assignments')
    op.drop_table('chunks')
    op.drop_table('moodle_courses')
    op.drop_table('learning_streak')
    op.drop_table('documents')
    op.drop_table('calendar_events')
    op.execute('DROP EXTENSION IF EXISTS vector')
