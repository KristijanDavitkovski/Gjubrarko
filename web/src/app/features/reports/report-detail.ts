import { DatePipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ReportDetail as Report, statusLabels } from '../../core/models';
import { trackingToken } from '../../core/tracking-tokens';

@Component({
  selector: 'app-report-detail',
  imports: [DatePipe],
  templateUrl: './report-detail.html',
  styleUrl: './report-detail.scss',
})
export class ReportDetail {
  private readonly api = inject(ApiService);

  // Bound from the route: /reports/:id?t=<tracking token>&created=1
  readonly id = input.required<string>();
  readonly t = input<string>();
  readonly created = input<string>();

  protected readonly statusLabels = statusLabels;
  protected readonly report = signal<Report | null>(null);
  protected readonly notFound = signal(false);

  constructor() {
    toObservable(this.id)
      .pipe(switchMap((id) => this.api.report(id, this.t() ?? trackingToken(id))))
      .subscribe({
        next: (r) => this.report.set(r),
        error: () => this.notFound.set(true),
      });
  }

  protected get trackingLink() {
    return `${location.origin}/reports/${this.id()}?t=${this.t()}`;
  }
}
