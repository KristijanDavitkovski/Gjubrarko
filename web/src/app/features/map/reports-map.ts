import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import * as L from 'leaflet';
import { ApiService, ReportFilter } from '../../core/api.service';
import { Category, Municipality, ReportListItem, ReportStatus, statusLabels } from '../../core/models';

const macedoniaCenter: L.LatLngTuple = [41.61, 21.75];

const statusColors: Record<ReportStatus, string> = {
  New: '#d9480f',
  Received: '#f08c00',
  InProgress: '#1971c2',
  Resolved: '#2f9e44',
  Rejected: '#868e96',
};

@Component({
  selector: 'app-reports-map',
  imports: [FormsModule, RouterLink],
  templateUrl: './reports-map.html',
  styleUrl: './reports-map.scss',
})
export class ReportsMap implements AfterViewInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly mapElement = viewChild.required<ElementRef<HTMLElement>>('map');
  private map?: L.Map;
  private markers = L.layerGroup();

  protected readonly statusLabels = statusLabels;
  protected readonly statuses = Object.keys(statusLabels) as ReportStatus[];
  protected readonly municipalities = signal<Municipality[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly reports = signal<ReportListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly filter = signal<ReportFilter>({ pageSize: 100 });

  constructor() {
    this.api.municipalities().subscribe((m) => this.municipalities.set(m));
    this.api.categories().subscribe((c) => this.categories.set(c));
    effect(() => {
      this.api.reports(this.filter()).subscribe((page) => {
        this.reports.set(page.items);
        this.total.set(page.total);
      });
    });
    effect(() => this.drawMarkers(this.reports()));
  }

  ngAfterViewInit() {
    this.map = L.map(this.mapElement().nativeElement).setView(macedoniaCenter, 8);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);
    this.markers.addTo(this.map);
    this.drawMarkers(this.reports());
  }

  ngOnDestroy() {
    this.map?.remove();
  }

  protected setFilter(change: Partial<ReportFilter>) {
    this.filter.update((f) => ({ ...f, ...change }));
    const m = this.municipalities().find((x) => x.id === change.municipalityId);
    if (m?.latitude && m.longitude) this.map?.setView([m.latitude, m.longitude], 12);
  }

  private drawMarkers(reports: ReportListItem[]) {
    if (!this.map) return;
    this.markers.clearLayers();
    for (const r of reports) {
      L.circleMarker([r.latitude, r.longitude], { radius: 8, color: statusColors[r.status], fillOpacity: 0.7 })
        .bindPopup(`<a href="/reports/${r.id}">${escapeHtml(r.title)}</a><br>${escapeHtml(r.municipality)} · ${statusLabels[r.status]}`)
        .addTo(this.markers);
    }
  }
}

function escapeHtml(text: string) {
  return text.replace(/[&<>"']/g, (c) => `&#${c.charCodeAt(0)};`);
}
