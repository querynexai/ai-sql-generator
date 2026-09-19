import { Component } from '@angular/core';
import { QueryGeneratorComponent } from './query-generator/query-generator'

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [QueryGeneratorComponent],
  template: `<app-query-generator />`
})
export class AppComponent {}