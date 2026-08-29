# Game Data Framework

This is the JSON-based Content Database Framework for the interrogation game. It serves as the single source of truth for all game content.

## Editing JSON Content

### 1. Where are the files located?
All game content resides in `Assets/StreamingAssets/GameData/`.

### 2. What each folder contains:
- **Cases/**: `.json` files defining the core scenario, references to suspect, questions, evidence, and the actual truth.
- **Suspects/**: `.json` files defining characters (name, description, etc.).
- **Questions/**: `.json` files defining questions and conditional responses.
- **Evidence/**: `.json` files defining clues found by investigators.
- **Endings/**: `.json` files defining possible conclusions and conditions for unlocking them.
- **Schemas/**: Contains JSON Schemas that enforce the structure of the files above.

### 3. JSON Schema Validation
The JSON Schema definitions provide strict structural validation. When you edit these files in VS Code, you will automatically receive autocomplete and real-time error highlighting if you type a property name wrong or omit a required field. This prevents syntax and structure errors before you ever open Unity.

### 4. VS Code Errors
Because of the `.vscode/settings.json` configuration in this project, VS Code is aware of which schema applies to which file. If you make a typo, VS Code will display a red squiggly line. 

### 5. Unity GameDataValidator
After structurally validating with JSON schema in VS Code, always run **Tools -> Validate Game Data** in the Unity Editor before testing. While the schema catches typos, the Unity Validator catches game-logic errors (like missing references, duplicate IDs, or a missing `isHeretic` value in the truth block).

### 6. How to add a new case
1. Create a suspect JSON in `Suspects/` with a unique ID.
2. Create relevant evidence JSONs in `Evidence/`.
3. Create relevant question JSONs in `Questions/`.
4. Create the case JSON in `Cases/`, referencing the above IDs.
5. Define the `truth` block reflecting the underlying facts.
6. Check VS Code for schema warnings.
7. Run the Unity Validation Tool to ensure no missing references or duplicate IDs.

### 7. How to create a new question
Questions require an `id`, `text`, and an array of `responses`. Create it in the `Questions/` folder:
```json
{
  "id": "QUESTION_EXAMPLE",
  "text": "What do you believe?",
  "responses": [
    {
      "answer": "I believe in the old ways."
    }
  ]
}
```

### 8. How truth facts work
The `truth` object in a Case file is intentionally flexible. You are NOT restricted to a specific list of booleans. You can invent any fact name as long as the value is a string, number, or boolean.
```json
"truth": {
  "isHeretic": false,
  "visitedChapel": true,
  "stoleTheRelic": true
}
```

### 9. How conditional responses work
You can restrict a question's response so it is only given if a certain truth fact matches a condition. Conditions are simple strings like `visitedChapel == true` or `occupation == "Farmer"`.
```json
"responses": [
  {
    "condition": "stoleTheRelic == true",
    "answer": "I don't know what relic you mean."
  }
]
```

## AI Editing Guidelines

When an AI agent is generating or editing content, they must strictly follow these rules:

- **Preserve existing IDs**: Do not change IDs simply to satisfy the schema.
- **Never modify unrelated cases**: Only edit the files explicitly requested.
- **Use stable human-readable IDs**: e.g., `CASE_002`, `SUSPECT_MARY`. Do not use GUIDs.
- **Validate references**: Make sure IDs you reference actually exist.
- **Keep truth facts flexible**: Invent new truth facts if the case calls for it. The schema allows it.
- **Use simple conditions**: `fact == true`, `fact == false`, `fact == "value"`. Do not invent complex boolean logic (no `&&` or `||`).
- **Run the Unity validator**: After generating content, always trigger the Unity validator to check references.
- **Avoid inventing fields**: Do not invent properties outside of the `truth` block. The root schema structures use `additionalProperties: false` and will reject unknown fields.
- **Keep each case internally coherent**: Facts, questions, and evidence should form a logical puzzle.
- **Never modify C# gameplay code**: When only content changes are requested, do not alter `InterrogationManager` or any other logic script.
