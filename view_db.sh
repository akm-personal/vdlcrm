#!/bin/bash

# SQLite Database Viewer Helper
# Usage: ./view_db.sh [table_name]

DB_PATH="/workspaces/vdlcrm/Vdlcrm.Web/vdlcrm.db"

if [ -z "$1" ]; then
    echo "=== All Tables in vdlcrm.db ==="
    sqlite3 "$DB_PATH" ".tables"
    echo ""
    echo "Usage: ./view_db.sh [table_name]"
    echo "Example: ./view_db.sh student_details"
else
    echo "=== Data from table: $1 ==="
    sqlite3 "$DB_PATH" ".mode column" ".headers on" "SELECT * FROM $1;"
fi
