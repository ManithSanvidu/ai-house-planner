import psycopg2
try:
    conn = psycopg2.connect("host=localhost port=5432 dbname=HousePlanner user=postgres password=postgres sslmode=prefer")
    cur = conn.cursor()
    cur.execute('SELECT "Id", "ConstructionPlan", "Status" FROM "WorkflowStates" ORDER BY "CreatedAt" DESC LIMIT 1;')
    row = cur.fetchone()
    if row:
        print(f'Workflow ID: {row[0]}')
        print(f'ConstructionPlan: {row[1]}')
        print(f'Status: {row[2]}')
    else:
        print('No workflow states found.')
    conn.close()
except Exception as e:
    print(e)
