// Synthetic CV generated in memory so CI does not depend on an ignored PDF.
export function createSmokePdf() {
  const lines = [
    'Alex Example - Software Engineer',
    'Synthetic candidate used only for automated integration tests.',
    'Five years developing REST APIs with C#, .NET and PostgreSQL.',
    'Experience with Docker, TypeScript, React and automated testing.',
    'Built internal dashboards and documented service integrations.',
    'Education: Bachelor of Computer Science. Languages: English, Spanish.',
  ];
  const escape = text => text.replace(/[\\()]/g, '\\$&');
  const stream = `BT /F1 12 Tf 50 780 Td 18 TL\n${lines.map(line => `(${escape(line)}) Tj T*`).join('\n')}\nET\n`;
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>',
    `<< /Length ${Buffer.byteLength(stream)} >>\nstream\n${stream}endstream`,
  ];
  let pdf = '%PDF-1.4\n';
  const offsets = [0];
  for (const [index, object] of objects.entries()) {
    offsets.push(Buffer.byteLength(pdf));
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  }
  const xref = Buffer.byteLength(pdf);
  pdf += `xref\n0 ${offsets.length}\n0000000000 65535 f \n`;
  pdf += offsets.slice(1).map(offset => `${String(offset).padStart(10, '0')} 00000 n \n`).join('');
  pdf += `trailer\n<< /Size ${offsets.length} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`;
  return Buffer.from(pdf, 'ascii');
}
