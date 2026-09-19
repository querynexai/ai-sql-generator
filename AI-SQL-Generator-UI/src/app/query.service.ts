import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface GenerateSqlResponse {
  sql: string;
  error?: string | null;
}

export interface ExecuteSqlResponse {
  success: boolean;
  data: Record<string, unknown>[];
  columns: string[];
  rowCount: number;
  error?: string | null;
}

@Injectable({ providedIn: 'root' })
export class QueryService {
  private readonly apiBase = environment.apiBase;

  constructor(private http: HttpClient) {}

  generateSql(query: string): Observable<GenerateSqlResponse> {
    return this.http.post<GenerateSqlResponse>(`${this.apiBase}/generate`, { query });
  }

  executeSql(sql: string): Observable<ExecuteSqlResponse> {
    return this.http.post<ExecuteSqlResponse>(`${this.apiBase}/execute`, { sql });
  }
}