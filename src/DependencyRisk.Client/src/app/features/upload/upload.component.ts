import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../../shared/services/api.service';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.scss'
})
export class UploadComponent {
  private api = inject(ApiService);
  private router = inject(Router);

  isDragging = false;
  isScanning = false;
  error = '';
  acceptedTypes = '.csproj,.json,.txt';

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.isDragging = true;
  }

  onDragLeave() {
    this.isDragging = false;
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.isDragging = false;
    const file = e.dataTransfer?.files[0];
    if (file) this.processFile(file);
  }

  onFileSelected(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.processFile(file);
  }

  useSample() {
    const sampleContent = `<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="AutoMapper" Version="12.0.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="Dapper" Version="2.1.28" />
    <PackageReference Include="Polly" Version="8.3.0" />
    <PackageReference Include="StackExchange.Redis" Version="2.7.23" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  </ItemGroup>
</Project>`;
    const blob = new Blob([sampleContent], { type: 'text/xml' });
    const file = new File([blob], 'SampleProject.csproj', { type: 'text/xml' });
    this.processFile(file);
  }

  private processFile(file: File) {
    this.error = '';
    this.isScanning = true;

    this.api.uploadFile(file).subscribe({
      next: (project) => {
        this.api.triggerScan(project.id).subscribe({
          next: (scan) => {
            this.isScanning = false;
            this.router.navigate(['/scan', scan.scanId]);
          },
          error: (err) => {
            this.isScanning = false;
            this.error = 'Scan failed: ' + (err.error?.detail ?? err.message);
          }
        });
      },
      error: (err) => {
        this.isScanning = false;
        this.error = 'Upload failed: ' + (err.error ?? err.message);
      }
    });
  }
}
