import psycopg2
from app.config import get_db_connection_string
conn = psycopg2.connect(get_db_connection_string())
cur = conn.cursor()
cur.execute('SELECT "ConstructionPlan" FROM "WorkflowStates" ORDER BY "CreatedAt" DESC LIMIT 1')
print(cur.fetchone())
