import os
import re

def replace_in_file(filepath, replacements):
    with open(filepath, 'r') as f:
        content = f.read()
    
    for old, new in replacements.items():
        content = re.sub(old, new, content)
        
    with open(filepath, 'w') as f:
        f.write(content)

# IntakeForm fixes
intake_replacements = {
    r'\bbg-white\b': 'bg-surface',
    r'\btext-gray-900\b': 'text-text-primary',
    r'\btext-gray-700\b': 'text-text-primary',
    r'\btext-gray-500\b': 'text-text-muted',
    r'\btext-gray-400\b': 'text-text-muted',
    r'\bbg-gray-50\b': 'bg-surface-elevated',
    r'\bbg-gray-100\b': 'bg-surface-muted',
    r'\bbg-gray-200\b': 'bg-surface-muted',
    r'\bborder-gray-100\b': 'border-border',
    r'\bborder-gray-200\b': 'border-border',
    r'\bborder-gray-300\b': 'border-border-strong',
}

replace_in_file('HousePlanner-Web/src/pages/IntakeForm.tsx', intake_replacements)

# ConstructionReadinessPage fixes
cr_replacements = {
    r'\btext-gray-700\b': 'text-text-secondary',
    r'\btext-gray-900 dark:text-text-primary\b': 'text-text-primary',
    r'\bbg-gray-50 dark:bg-surface-elevated\b': 'bg-surface-elevated',
    r'\bbg-gray-50/50 dark:bg-surface-elevated\b': 'bg-surface-elevated',
    r'\bbg-gray-50 dark:bg-gray-800/50\b': 'bg-surface-muted',
    r'\bbg-gray-50\b': 'bg-surface-elevated',
    r'\bbg-white dark:bg-surface\b': 'bg-surface',
    r'\bbg-white dark:bg-surface-elevated\b': 'bg-surface-elevated',
    r'\bborder-gray-200 dark:border-border-strong\b': 'border-border-strong',
    r'\btext-gray-900\b': 'text-text-primary',
    r'\btext-gray-500\b': 'text-text-muted',
}
replace_in_file('HousePlanner-Web/src/pages/ConstructionReadinessPage.tsx', cr_replacements)

# Auth pages fixes
auth_replacements = {
    r'\btext-gray-900 dark:text-text-primary\b': 'text-text-primary',
    r'\btext-gray-900 dark:text-white\b': 'text-text-primary',
    r'\btext-gray-700 dark:text-gray-300\b': 'text-text-secondary',
    r'\btext-gray-500 dark:text-gray-400\b': 'text-text-muted',
    r'\bbg-gray-50 dark:bg-gray-800/50\b': 'bg-surface-elevated',
    r'\bborder-gray-200 dark:border-gray-700\b': 'border-border',
    r'\bhover:bg-gray-50 dark:hover:bg-gray-700\b': 'hover:bg-surface-muted',
}
replace_in_file('HousePlanner-Web/src/pages/LoginPage.tsx', auth_replacements)
replace_in_file('HousePlanner-Web/src/pages/RegisterPage.tsx', auth_replacements)

# HomePage fixes
home_replacements = {
    r'\btext-gray-900 dark:text-text-primary\b': 'text-text-primary',
    r'\btext-gray-800 dark:text-gray-300\b': 'text-text-secondary',
    r'\btext-gray-700 dark:text-gray-300\b': 'text-text-primary',
    r'\bbg-gray-50 dark:bg-gray-800/30\b': 'bg-surface-elevated',
    r'\bbg-gray-50 dark:bg-gray-800/50\b': 'bg-surface-elevated',
    r'\bborder-gray-100 dark:border-border-strong\b': 'border-border',
    r'\btext-text-secondary hover:text-gray-700 dark:hover:text-gray-200\b': 'text-text-secondary hover:text-text-primary',
    r'\btext-text-muted hover:text-gray-900 dark:hover:text-text-primary\b': 'text-text-muted hover:text-text-primary',
    r'\btext-gray-900 dark:text-white\b': 'text-text-primary',
}
replace_in_file('HousePlanner-Web/src/pages/HomePage.tsx', home_replacements)

