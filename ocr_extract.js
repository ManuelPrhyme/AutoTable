const fs = require('fs');

async function main() {
  const data = fs.readFileSync('Assets/modal-walmart-old-checkout.jpg');
  console.log('Image size:', data.length, 'bytes');

  // Try 0x0.st - simple file hosting via PUT
  try {
    const blob = new Blob([data], { type: 'image/jpeg' });
    const fd = new FormData();
    fd.append('file', blob, 'modal.jpg');
    const res = await fetch('https://0x0.st', {
      method: 'POST',
      body: fd
    });
    const url = (await res.text()).trim();
    if (url.startsWith('http')) {
      console.log('UPLOAD_URL:', url);
      process.exit(0);
    }
    console.log('0x0.st response:', url);
  } catch (e) {
    console.error('0x0.st error:', e.message);
  }

  // Try file.io
  try {
    const blob = new Blob([data], { type: 'image/jpeg' });
    const fd = new FormData();
    fd.append('file', blob, 'modal.jpg');
    const res = await fetch('https://file.io', {
      method: 'POST',
      body: fd
    });
    const json = await res.json();
    if (json.success && json.link) {
      console.log('UPLOAD_URL:', json.link);
      process.exit(0);
    }
    console.log('file.io response:', JSON.stringify(json));
  } catch (e) {
    console.error('file.io error:', e.message);
  }

  // Try imgur
  try {
    const b64 = data.toString('base64');
    const res = await fetch('https://api.imgur.com/image', {
      method: 'POST',
      headers: {
        'Authorization': 'Client-ID 5285d3ab15d4f14',
        'Accept': 'application/json'
      },
      body: new URLSearchParams({ image: b64, type: 'base64' })
    });
    const json = await res.json();
    if (json.data && json.data.link) {
      console.log('UPLOAD_URL:', json.data.link);
      process.exit(0);
    }
    console.log('imgur response:', JSON.stringify(json));
  } catch (e) {
    console.error('imgur error:', e.message);
  }

  console.log('ERROR: All upload methods failed');
  process.exit(1);
}

main();