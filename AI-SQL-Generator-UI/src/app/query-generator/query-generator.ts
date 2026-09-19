import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QueryService } from '../query.service';

@Component({
  selector: 'app-query-generator',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './query-generator.html',
  styleUrls: ['./query-generator.css']
})
export class QueryGeneratorComponent {
  private readonly queryService = inject(QueryService);

  naturalQuery = '';

  readonly generatedSql = signal('');
  readonly results = signal<Record<string, unknown>[]>([]);
  readonly columns = signal<string[]>([]);
  readonly rowCount = signal(0);
  readonly errorMessage = signal('');

  readonly loadingGenerate = signal(false);
  readonly loadingExecute = signal(false);

  readonly examples: string[] = [
    'Show me the top 5 most expensive products',
    'How many customers do we have in India?',
    'What is the total revenue from delivered orders?',
    'List all products with price greater than 50000',
    'Show the number of orders per status',
    'Which customer has placed the most orders?'
  ];

  generateSql(): void {
    const query = this.naturalQuery.trim();
    if (!query) return;

    this.resetOutput();
    this.loadingGenerate.set(true);

    this.queryService.generateSql(query).subscribe({
      next: (res) => {
        this.loadingGenerate.set(false);
        if (res.error) {
          this.errorMessage.set(res.error);
        } else {
          this.generatedSql.set(res.sql);
        }
      },
      error: (err) => {
        this.loadingGenerate.set(false);
        this.errorMessage.set('Failed to generate SQL. Is the backend running?');
        console.error(err);
      }
    });
  }

  executeSql(): void {
    const sql = this.generatedSql();
    if (!sql.trim()) return;

    this.errorMessage.set('');
    this.loadingExecute.set(true);

    this.queryService.executeSql(sql).subscribe({
      next: (res) => {
        this.loadingExecute.set(false);
        if (!res.success) {
          this.errorMessage.set(res.error ?? 'Query execution failed.');
          this.results.set([]);
          this.columns.set([]);
          this.rowCount.set(0);
        } else {
          this.results.set(res.data);
          this.columns.set(res.columns);
          this.rowCount.set(res.rowCount);
        }
      },
      error: (err) => {
        this.loadingExecute.set(false);
        this.errorMessage.set('Failed to execute SQL.');
        console.error(err);
      }
    });
  }

  clearAll(): void {
    this.naturalQuery = '';
    this.resetOutput();
  }

  useExample(example: string): void {
    this.naturalQuery = example;
    this.generateSql();
  }

  private resetOutput(): void {
    this.generatedSql.set('');
    this.results.set([]);
    this.columns.set([]);
    this.rowCount.set(0);
    this.errorMessage.set('');
  }

  getCellValue(row: Record<string, unknown>, col: string): string {
    const val = row[col];
    if (val === null || val === undefined) return '—';
    if (typeof val === 'object') return JSON.stringify(val);
    return String(val);
  }
}