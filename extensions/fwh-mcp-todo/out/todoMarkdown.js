"use strict";
/**
 * Round-trip between TODO item and YAML-front-matter document for the editor.
 *
 * Format: YAML front matter (between --- delimiters) for structured fields;
 * body below the closing --- is the description (one line per array element).
 * Priority defaults to 'low' if not specified.
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.blankTodoTemplate = blankTodoTemplate;
exports.todoToMarkdown = todoToMarkdown;
exports.markdownToNewTodoFields = markdownToNewTodoFields;
exports.markdownToUpdateBody = markdownToUpdateBody;
/**
 * Returns a blank YAML front matter template for creating a new TODO item.
 */
function blankTodoTemplate() {
    return [
        '---',
        'id: NEW-TODO',
        'section: mvp-app',
        'priority: low',
        'estimate: ',
        'depends-on: []',
        '---',
        '',
        '# ',
        '',
        'Description goes here.',
        '',
        '## Technical Details',
        '',
        '- ',
        '',
        '## Implementation Tasks',
        '',
        '- [ ] ',
        '',
    ].join('\n');
}
/** Serialize a TODO item to YAML front matter + markdown body. */
function todoToMarkdown(item) {
    // Front matter: metadata only (no title, technical-details, or implementation-tasks)
    const fm = ['---'];
    fm.push(`id: ${item.id}`);
    fm.push(`section: ${item.section ?? ''}`);
    fm.push(`priority: ${item.priority ?? ''}`);
    if (item.done)
        fm.push('done: true');
    if (item.estimate)
        fm.push(`estimate: ${yamlScalar(item.estimate)}`);
    if (item.note)
        fm.push(`note: ${yamlScalar(item.note)}`);
    if (item.completedDate)
        fm.push(`completed: ${item.completedDate}`);
    if (item.doneSummary)
        fm.push(`done-summary: ${yamlScalar(item.doneSummary)}`);
    if (item.remaining)
        fm.push(`remaining: ${yamlScalar(item.remaining)}`);
    // depends-on
    if (item.dependsOn?.length) {
        fm.push('depends-on:');
        item.dependsOn.forEach((d) => fm.push(`  - ${d}`));
    }
    // functional-requirements
    if (item.functionalRequirements?.length) {
        fm.push('functional-requirements:');
        item.functionalRequirements.forEach((fr) => fm.push(`  - ${fr}`));
    }
    // technical-requirements
    if (item.technicalRequirements?.length) {
        fm.push('technical-requirements:');
        item.technicalRequirements.forEach((tr) => fm.push(`  - ${tr}`));
    }
    fm.push('---');
    // Body: title as H1, description, then H2 sections
    const body = [''];
    body.push(`# ${item.title ?? ''}`);
    body.push('');
    if (item.description?.length) {
        item.description.forEach((d) => body.push(d));
        body.push('');
    }
    if (item.technicalDetails?.length) {
        body.push('## Technical Details');
        body.push('');
        item.technicalDetails.forEach((d) => body.push(`- ${d}`));
        body.push('');
    }
    if (item.implementationTasks?.length) {
        body.push('## Implementation Tasks');
        body.push('');
        item.implementationTasks.forEach((t) => {
            const checkbox = t.done ? '[x]' : '[ ]';
            body.push(`- ${checkbox} ${t.task}`);
        });
        body.push('');
    }
    return fm.join('\n') + body.join('\n');
}
/**
 * Parse a YAML front matter + markdown body document into fields for new-todo creation.
 * Title comes from H1, technical details and implementation tasks from H2 sections.
 * Priority defaults to 'low' if not specified.
 */
function markdownToNewTodoFields(markdown) {
    const { frontMatter, body } = splitFrontMatter(markdown);
    const fm = parseFrontMatter(frontMatter);
    const sections = parseBody(body);
    return {
        id: fm.id ?? 'NEW-TODO',
        title: sections.title ?? fm.title ?? '',
        section: fm.section ?? 'mvp-app',
        priority: fm.priority ?? 'low',
        description: sections.description.length ? sections.description : undefined,
        technicalDetails: sections.technicalDetails.length ? sections.technicalDetails : fm.technicalDetails,
        implementationTasks: sections.implementationTasks.length ? sections.implementationTasks : fm.implementationTasks,
        dependsOn: fm.dependsOn,
        functionalRequirements: fm.functionalRequirements,
        technicalRequirements: fm.technicalRequirements,
    };
}
/**
 * Parse YAML front matter + markdown body document back into an update body.
 * Title comes from H1, technical details and implementation tasks from H2 sections.
 */
function markdownToUpdateBody(markdown) {
    const { frontMatter, body } = splitFrontMatter(markdown);
    const fm = parseFrontMatter(frontMatter);
    const sections = parseBody(body);
    // Copy all front matter fields except id
    const updateBody = {};
    if (fm.priority !== undefined)
        updateBody.priority = fm.priority;
    if (fm.section !== undefined)
        updateBody.section = fm.section;
    if (fm.done !== undefined)
        updateBody.done = fm.done;
    if (fm.estimate !== undefined)
        updateBody.estimate = fm.estimate;
    if (fm.note !== undefined)
        updateBody.note = fm.note;
    if (fm.completedDate !== undefined)
        updateBody.completedDate = fm.completedDate;
    if (fm.doneSummary !== undefined)
        updateBody.doneSummary = fm.doneSummary;
    if (fm.remaining !== undefined)
        updateBody.remaining = fm.remaining;
    if (fm.dependsOn !== undefined)
        updateBody.dependsOn = fm.dependsOn;
    if (fm.functionalRequirements !== undefined)
        updateBody.functionalRequirements = fm.functionalRequirements;
    if (fm.technicalRequirements !== undefined)
        updateBody.technicalRequirements = fm.technicalRequirements;
    // Body sections override front matter equivalents
    updateBody.title = sections.title ?? fm.title;
    if (sections.description.length)
        updateBody.description = sections.description;
    if (sections.technicalDetails.length)
        updateBody.technicalDetails = sections.technicalDetails;
    else if (fm.technicalDetails !== undefined)
        updateBody.technicalDetails = fm.technicalDetails;
    if (sections.implementationTasks.length)
        updateBody.implementationTasks = sections.implementationTasks;
    else if (fm.implementationTasks !== undefined)
        updateBody.implementationTasks = fm.implementationTasks;
    return updateBody;
}
/** Split document into front matter lines and body lines (preserving blanks). */
function splitFrontMatter(doc) {
    const lines = doc.split(/\r?\n/);
    let start = -1;
    let end = -1;
    for (let i = 0; i < lines.length; i++) {
        if (lines[i].trim() === '---') {
            if (start < 0)
                start = i;
            else {
                end = i;
                break;
            }
        }
    }
    if (start < 0 || end < 0) {
        // No front matter delimiters — treat entire doc as body (backwards compat)
        return { frontMatter: [], body: lines };
    }
    const fmLines = lines.slice(start + 1, end);
    const bodyLines = lines.slice(end + 1);
    return { frontMatter: fmLines, body: bodyLines };
}
/** Minimal YAML parser for flat keys and simple lists (depends-on only). */
function parseFrontMatter(lines) {
    const fm = {};
    let i = 0;
    while (i < lines.length) {
        const line = lines[i];
        // Skip blank lines
        if (line.trim() === '') {
            i++;
            continue;
        }
        // Top-level key: value
        const kvMatch = line.match(/^([a-z][a-z0-9-]*)\s*:\s*(.*)$/i);
        if (!kvMatch) {
            i++;
            continue;
        }
        const key = kvMatch[1].toLowerCase();
        const value = kvMatch[2].trim();
        // Check if next lines are list items (indented with -)
        const isListStart = value === '' || value === '[]';
        // Backward compat: still parse implementation-tasks and technical-details from front matter
        if (isListStart && key === 'implementation-tasks') {
            fm.implementationTasks = [];
            i++;
            while (i < lines.length && /^\s+/.test(lines[i])) {
                const taskMatch = lines[i].match(/^\s+-\s*task:\s*(.*)$/i);
                if (taskMatch) {
                    const taskText = unquote(taskMatch[1].trim());
                    let done = false;
                    if (i + 1 < lines.length) {
                        const doneMatch = lines[i + 1].match(/^\s+done:\s*(true|false)$/i);
                        if (doneMatch) {
                            done = doneMatch[1].toLowerCase() === 'true';
                            i++;
                        }
                    }
                    fm.implementationTasks.push({ task: taskText, done });
                }
                i++;
            }
            continue;
        }
        if (isListStart && (key === 'depends-on' || key === 'technical-details' || key === 'functional-requirements' || key === 'technical-requirements')) {
            const arr = [];
            i++;
            while (i < lines.length && /^\s+-\s/.test(lines[i])) {
                const itemMatch = lines[i].match(/^\s+-\s*(.+)$/);
                if (itemMatch)
                    arr.push(unquote(itemMatch[1].trim()));
                i++;
            }
            if (key === 'depends-on')
                fm.dependsOn = arr;
            else if (key === 'functional-requirements')
                fm.functionalRequirements = arr;
            else if (key === 'technical-requirements')
                fm.technicalRequirements = arr;
            else
                fm.technicalDetails = arr;
            continue;
        }
        // Inline list: key: [a, b, c]
        if (value.startsWith('[') && value.endsWith(']')) {
            const inner = value.slice(1, -1).trim();
            const arr = inner ? inner.split(',').map((s) => unquote(s.trim())).filter(Boolean) : [];
            if (key === 'depends-on')
                fm.dependsOn = arr;
            else if (key === 'functional-requirements')
                fm.functionalRequirements = arr;
            else if (key === 'technical-requirements')
                fm.technicalRequirements = arr;
            else if (key === 'technical-details')
                fm.technicalDetails = arr;
            i++;
            continue;
        }
        // Scalar values
        const v = unquote(value);
        switch (key) {
            case 'id':
                fm.id = v;
                break;
            case 'title':
                fm.title = v;
                break;
            case 'section':
                fm.section = v;
                break;
            case 'priority':
                fm.priority = v.toLowerCase();
                break;
            case 'done':
                fm.done = /^(true|1|yes)$/i.test(v);
                break;
            case 'estimate':
                fm.estimate = v;
                break;
            case 'note':
                fm.note = v;
                break;
            case 'completed':
                fm.completedDate = v;
                break;
            case 'done-summary':
                fm.doneSummary = v;
                break;
            case 'remaining':
                fm.remaining = v;
                break;
        }
        i++;
    }
    return fm;
}
/** Parse markdown body for H1 title, description, and H2 sections. */
function parseBody(lines) {
    const result = {
        description: [],
        technicalDetails: [],
        implementationTasks: [],
    };
    let currentSection = 'description';
    for (const line of lines) {
        const trimmed = line.trim();
        // H1 = title
        const h1 = trimmed.match(/^#\s+(.+)$/);
        if (h1) {
            result.title = h1[1].trim();
            currentSection = 'description';
            continue;
        }
        // H2 = section header
        const h2 = trimmed.match(/^##\s+(.+)$/);
        if (h2) {
            const heading = h2[1].trim().toLowerCase();
            if (heading.includes('technical'))
                currentSection = 'technical-details';
            else if (heading.includes('implementation') || heading.includes('task'))
                currentSection = 'implementation-tasks';
            else
                currentSection = 'description';
            continue;
        }
        // Skip blank lines
        if (trimmed === '')
            continue;
        if (currentSection === 'technical-details') {
            // List items: strip leading "- "
            const bullet = trimmed.match(/^-\s+(.+)$/);
            result.technicalDetails.push(bullet ? bullet[1] : trimmed);
        }
        else if (currentSection === 'implementation-tasks') {
            // Checkbox items: - [x] or - [ ]
            const taskMatch = trimmed.match(/^-\s+\[([ xX])\]\s+(.+)$/);
            if (taskMatch) {
                result.implementationTasks.push({
                    task: taskMatch[2],
                    done: taskMatch[1].toLowerCase() === 'x',
                });
            }
            else {
                // Plain list item without checkbox — treat as undone task
                const bullet = trimmed.match(/^-\s+(.+)$/);
                if (bullet) {
                    result.implementationTasks.push({ task: bullet[1], done: false });
                }
            }
        }
        else {
            result.description.push(trimmed);
        }
    }
    return result;
}
/** Remove surrounding quotes from a YAML scalar. */
function unquote(s) {
    if ((s.startsWith('"') && s.endsWith('"')) || (s.startsWith("'") && s.endsWith("'")))
        return s.slice(1, -1);
    return s;
}
/** Quote a YAML scalar if it contains special characters. */
function yamlScalar(s) {
    if (/[:#\[\]{}&*!|>'"%@`]/.test(s) || s.includes('\n'))
        return `"${s.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"`;
    return s;
}
//# sourceMappingURL=todoMarkdown.js.map