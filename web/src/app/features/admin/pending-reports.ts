import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { PendingReport } from '../../core/models';

@Component({
  selector: 'app-pending-reports',
  imports: [DatePipe, RouterLink],
  template: `
    <h1>Анонимни пријави за одобрување</h1>
    <table>
      <thead>
        <tr><th>Број</th><th>Наслов</th><th>Општина</th><th>Фотографии</th><th>Пристигната</th><th></th></tr>
      </thead>
      <tbody>
        @for (r of pending(); track r.id) {
          <tr>
            <td>{{ r.publicNumber }}</td>
            <td><a [routerLink]="['/reports', r.id]">{{ r.title }}</a></td>
            <td>{{ r.municipality }}</td>
            <td>{{ r.photoCount }}</td>
            <td>{{ r.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
            <td class="actions">
              <button class="primary" (click)="approve(r.id)">Одобри</button>
              <button (click)="hide(r.id)">Отфрли</button>
            </td>
          </tr>
        } @empty {
          <tr><td colspan="6">Нема пријави што чекаат.</td></tr>
        }
      </tbody>
    </table>
  `,
})
export class PendingReports {
  private readonly api = inject(ApiService);
  protected readonly pending = signal<PendingReport[]>([]);

  constructor() {
    this.load();
  }

  protected approve(id: string) {
    this.api.approveReport(id).subscribe(() => this.load());
  }

  protected hide(id: string) {
    this.api.hideReport(id).subscribe(() => this.load());
  }

  private load() {
    this.api.pendingReports().subscribe((list) => this.pending.set(list));
  }
}
