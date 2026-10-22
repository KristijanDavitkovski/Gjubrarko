import { AfterViewInit, Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Authority, Category, Municipality, Severity } from '../../core/models';
import { saveTrackingToken } from '../../core/tracking-tokens';

const maxPhotos = 5;
const maxPhotoBytes = 10 * 1024 * 1024;

@Component({
  selector: 'app-create-report',
  imports: [ReactiveFormsModule],
  templateUrl: './create-report.html',
  styleUrl: './create-report.scss',
})
export class CreateReport implements AfterViewInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  private readonly mapElement = viewChild.required<ElementRef<HTMLElement>>('map');
  private map?: L.Map;
  private pin?: L.CircleMarker;

  protected readonly municipalities = signal<Municipality[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly authorities = signal<Authority[]>([]);
  protected readonly photos = signal<File[]>([]);
  protected readonly location = signal<L.LatLng | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly severities: { value: Severity; label: string }[] = [
    { value: 'Low', label: 'Мало' },
    { value: 'Medium', label: 'Средно' },
    { value: 'High', label: 'Сериозно' },
  ];

  protected readonly form = inject(FormBuilder).nonNullable.group({
    municipalityId: [0, Validators.min(1)],
    categoryId: [0, Validators.min(1)],
    title: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', Validators.maxLength(4000)],
    addressText: [''],
    severity: ['Medium' as Severity],
    authorityIds: [[] as number[]],
    anonymous: [false],
    contactEmail: ['', Validators.email],
  });

  protected readonly willBeAnonymous = computed(() => !this.auth.isSignedIn());

  constructor() {
    this.api.municipalities().subscribe((m) => this.municipalities.set(m));
    this.api.categories().subscribe((c) => this.categories.set(c));

    this.form.controls.municipalityId.valueChanges.subscribe((id) => {
      const m = this.municipalities().find((x) => x.id === id);
      if (!m) return;
      if (m.latitude && m.longitude) this.map?.setView([m.latitude, m.longitude], 13);
      this.api.authorities(m.slug).subscribe((list) => {
        this.authorities.set(list);
        this.suggestAuthorities();
      });
    });
    this.form.controls.categoryId.valueChanges.subscribe(() => this.suggestAuthorities());
  }

  ngAfterViewInit() {
    this.map = L.map(this.mapElement().nativeElement).setView([41.61, 21.75], 8);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);
    this.map.on('click', (e: L.LeafletMouseEvent) => this.setPin(e.latlng));
  }

  ngOnDestroy() {
    this.map?.remove();
  }

  protected useMyLocation() {
    navigator.geolocation?.getCurrentPosition((p) => {
      const at = L.latLng(p.coords.latitude, p.coords.longitude);
      this.setPin(at);
      this.map?.setView(at, 16);
    });
  }

  protected toggleAuthority(id: number, checked: boolean) {
    const current = this.form.controls.authorityIds.value;
    this.form.controls.authorityIds.setValue(checked ? [...current, id] : current.filter((x) => x !== id));
  }

  protected addPhotos(event: Event) {
    const input = event.target as HTMLInputElement;
    const picked = Array.from(input.files ?? []);
    input.value = '';
    const tooBig = picked.find((f) => f.size > maxPhotoBytes);
    if (tooBig) {
      this.error.set(`${tooBig.name} е поголема од 10 MB.`);
      return;
    }
    this.photos.update((p) => [...p, ...picked].slice(0, maxPhotos));
  }

  protected removePhoto(index: number) {
    this.photos.update((p) => p.filter((_, i) => i !== index));
  }

  protected submit() {
    const at = this.location();
    if (this.form.invalid || !at) {
      this.error.set(at ? 'Пополнете ги задолжителните полиња.' : 'Означете ја локацијата на мапата.');
      return;
    }
    const v = this.form.getRawValue();
    if (v.authorityIds.length === 0) {
      this.error.set('Изберете барем една надлежна институција.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.api
      .createReport(
        {
          municipalityId: v.municipalityId,
          categoryId: v.categoryId,
          title: v.title,
          description: v.description || null,
          latitude: at.lat,
          longitude: at.lng,
          addressText: v.addressText || null,
          severity: v.severity,
          authorityIds: v.authorityIds,
          anonymous: this.willBeAnonymous() || v.anonymous,
          contactEmail: v.contactEmail || null,
        },
        this.photos(),
        null, // TODO: Cloudflare Turnstile token once the site key is configured
      )
      .subscribe({
        next: (result) => {
          if (result.trackingToken) saveTrackingToken(result.id, result.trackingToken);
          this.router.navigate(['/reports', result.id], {
            queryParams: result.trackingToken ? { t: result.trackingToken, created: 1 } : { created: 1 },
          });
        },
        error: (e) => {
          this.submitting.set(false);
          this.error.set(e.error?.detail ?? 'Пријавата не успеа. Обидете се повторно.');
        },
      });
  }

  private setPin(at: L.LatLng) {
    this.location.set(at);
    if (!this.map) return;
    this.pin?.remove();
    this.pin = L.circleMarker(at, { radius: 10, color: '#d9480f', fillOpacity: 0.8 }).addTo(this.map);
  }

  // Ticks the authorities whose type matches the category's default; the citizen can still change the choice.
  private suggestAuthorities() {
    const category = this.categories().find((c) => c.id === this.form.controls.categoryId.value);
    if (!category) return;
    const suggested = this.authorities().filter((a) => a.authorityTypeId === category.defaultAuthorityTypeId).map((a) => a.id);
    this.form.controls.authorityIds.setValue(suggested);
  }
}
