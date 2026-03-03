"""email_ews_fields

Revision ID: b4c8d2e3f5a6
Revises: a3b7c9d1e2f4
Create Date: 2026-03-04 12:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa

# revision identifiers, used by Alembic.
revision: str = 'b4c8d2e3f5a6'
down_revision: Union[str, None] = 'a3b7c9d1e2f4'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # Add email_username column
    op.add_column('app_settings', sa.Column(
        'email_username', sa.String(length=200), nullable=False, server_default=''
    ))
    # Rename email_imap_server -> email_server
    op.alter_column('app_settings', 'email_imap_server', new_column_name='email_server')


def downgrade() -> None:
    op.alter_column('app_settings', 'email_server', new_column_name='email_imap_server')
    op.drop_column('app_settings', 'email_username')
