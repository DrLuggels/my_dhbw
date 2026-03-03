"""add_settings_and_token_usage

Revision ID: a3b7c9d1e2f4
Revises: 9f148d9a1bad
Create Date: 2026-03-03 23:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa

# revision identifiers, used by Alembic.
revision: str = 'a3b7c9d1e2f4'
down_revision: Union[str, None] = '9f148d9a1bad'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.create_table('app_settings',
        sa.Column('id', sa.Integer(), nullable=False),
        sa.Column('ai_provider', sa.String(length=20), nullable=False, server_default='direct'),
        sa.Column('github_token', sa.Text(), nullable=False, server_default=''),
        sa.Column('openai_api_key', sa.Text(), nullable=False, server_default=''),
        sa.Column('anthropic_api_key', sa.Text(), nullable=False, server_default=''),
        sa.Column('gemini_api_key', sa.Text(), nullable=False, server_default=''),
        sa.Column('openai_model', sa.String(length=100), nullable=False, server_default='gpt-4.1-mini'),
        sa.Column('anthropic_model', sa.String(length=100), nullable=False, server_default='claude-sonnet-4-6'),
        sa.Column('gemini_model', sa.String(length=100), nullable=False, server_default='gemini-2.5-flash'),
        sa.Column('embedding_model', sa.String(length=100), nullable=False, server_default='text-embedding-3-small'),
        sa.Column('embedding_dimensions', sa.Integer(), nullable=False, server_default='1536'),
        sa.Column('moodle_base_url', sa.String(length=500), nullable=False, server_default='https://moodle.dhbw-ravensburg.de'),
        sa.Column('moodle_token', sa.Text(), nullable=False, server_default=''),
        sa.Column('email_address', sa.String(length=200), nullable=False, server_default=''),
        sa.Column('email_password', sa.Text(), nullable=False, server_default=''),
        sa.Column('email_imap_server', sa.String(length=200), nullable=False, server_default=''),
        sa.Column('rapla_calendar_url', sa.Text(), nullable=False, server_default=''),
        sa.Column('updated_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
        sa.PrimaryKeyConstraint('id')
    )

    # Insert default row
    op.execute(
        "INSERT INTO app_settings (id) VALUES (1)"
    )

    op.create_table('token_usage',
        sa.Column('id', sa.Integer(), autoincrement=True, nullable=False),
        sa.Column('provider', sa.String(length=30), nullable=False),
        sa.Column('model', sa.String(length=100), nullable=False),
        sa.Column('input_tokens', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('output_tokens', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('task_type', sa.String(length=50), nullable=False),
        sa.Column('created_at', sa.DateTime(timezone=True), server_default=sa.text('now()'), nullable=False),
        sa.PrimaryKeyConstraint('id')
    )


def downgrade() -> None:
    op.drop_table('token_usage')
    op.drop_table('app_settings')
