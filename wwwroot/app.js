const API_URL = "http://localhost:5006/api/Zgrada"; 
let trenutnaZgradaId = null;
let sveZgrade = [];

// 1. Inicijalizacija i Event Listeners
document.addEventListener('DOMContentLoaded', () => {
    fetchZgrade();
    
    const formaZgrada = document.getElementById('form-nova-zgrada');
    if (formaZgrada) {
        formaZgrada.addEventListener('submit', async (e) => {
            e.preventDefault();
            const data = {
                adresa: document.getElementById('adresa').value,
                brojStanova: parseInt(document.getElementById('brojStanova').value),
                budzetZgrade: parseFloat(document.getElementById('budzet').value)
            };

            try {
                const res = await fetch(API_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });

                if(res.ok) {
                    Swal.fire('Uspešno!', 'Zgrada je dodata u sistem.', 'success');
                    e.target.reset();
                    showSection('view-zgrade', document.querySelector('.nav-links li'));
                    fetchZgrade();
                }
            } catch (err) {
                Swal.fire('Greška', 'Server nije dostupan.', 'error');
            }
        });
    }
});

// 2. Dashboard statistika
async function fetchZgrade() {
    try {
        const res = await fetch(API_URL);
        sveZgrade = await res.json();
        
        document.getElementById('stat-zgrade').innerText = sveZgrade.length;
        const totalBudzet = sveZgrade.reduce((acc, z) => acc + z.budzetZgrade, 0);
        document.getElementById('stat-budzet').innerText = totalBudzet.toLocaleString() + " RSD";
        
        let aktivniKvarovi = 0;
        sveZgrade.forEach(z => {
            aktivniKvarovi += z.kvarovi.filter(k => k.status !== 'Resen').length;
        });
        document.getElementById('stat-kvarovi').innerText = aktivniKvarovi;

        renderZgrade(sveZgrade);
    } catch (err) {
        console.error("Greška pri učitavanju:", err);
    }
}

// 3. Prikaz kartica zgrada
function renderZgrade(data) {
    const container = document.getElementById('zgrade-list');
    container.innerHTML = data.map(z => `
        <div class="card">
            <h3><i class="fas fa-location-dot" style="color:var(--primary)"></i> ${z.adresa}</h3>
            <div class="card-info">
                <p><i class="fas fa-door-open"></i> Stanova: ${z.brojStanova}</p>
                <p><i class="fas fa-coins"></i> Budžet: ${z.budzetZgrade.toLocaleString()} RSD</p>
                <p><i class="fas fa-triangle-exclamation"></i> Kvarovi: ${z.kvarovi.length}</p>
            </div>
            <button onclick="otvoriKvarove('${z.id}', '${z.adresa}')" class="btn-primary">
                Detalji i Kvarovi
            </button>
        </div>
    `).join('');
}

// 4. Modal za kvarove
async function otvoriKvarove(id, adresa) {
    trenutnaZgradaId = id;
    document.getElementById('modal-zgrada-naslov').innerText = adresa;
    document.getElementById('modal-kvarovi').classList.remove('hidden');
    
    const statRes = await fetch(`${API_URL}/${id}/statistika`);
    const stat = await statRes.json();
    document.getElementById('statistika-trosak').innerHTML = `
        <div style="background: rgba(99,102,241,0.1); padding: 15px; border-radius: 12px; border-left: 4px solid var(--primary); margin-bottom: 15px;">
            Ukupan trošak održavanja: <strong>${stat.ukupnoTroskova.toLocaleString()} RSD</strong>
        </div>`;

    osveziListuKvarova();
}

// 5. Osvežavanje tabele kvarova
async function osveziListuKvarova() {
    const res = await fetch(API_URL);
    const zgrade = await res.json();
    const zgrada = zgrade.find(z => z.id === trenutnaZgradaId);
    
    const tbody = document.getElementById('kvarovi-body');
    if (zgrada && zgrada.kvarovi) {
        tbody.innerHTML = zgrada.kvarovi.map(k => `
            <tr>
                <td>
                    <div style="font-weight:600">${k.hitno ? '🔴 ' : ''}${k.opis}</div>
                    <div style="font-size:11px; color:var(--text-dim)">${new Date(k.datum).toLocaleDateString()}</div>
                </td>
                <td>${k.trosak.toLocaleString()}</td>
                <td><span class="status-tag ${k.status === 'Resen' ? 'status-resen' : 'status-prijavljen'}">${k.status}</span></td>
                <td>
                    ${k.status !== 'Resen' ? `<button onclick="resiKvar('${k.id}')" class="btn-primary" style="padding:5px 10px; font-size:12px">Reši</button>` : '✅'}
                </td>
            </tr>
        `).join('');
    }
}


async function prijaviKvar() {
    const opisElement = document.getElementById('kvar-opis');
    const trosakElement = document.getElementById('kvar-trosak');
    const hitnoElement = document.getElementById('kvar-hitno');

    
    const dto = {
        opis: opisElement.value.trim(),
        
        trosak: parseFloat(trosakElement.value) || 0, 
        
        hitno: hitnoElement.checked ? true : false 
    };

    console.log("Šaljem na server:", dto); 

    if (!dto.opis) {
        return Swal.fire('Pažnja', 'Unesite opis kvara!', 'warning');
    }

    try {
        const res = await fetch(`${API_URL}/${trenutnaZgradaId}/kvar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (res.ok) {
            Swal.fire('Uspešno', 'Kvar je dodat!', 'success');
            
            
            opisElement.value = '';
            trosakElement.value = '';
            hitnoElement.checked = false;

            await osveziListuKvarova();
            fetchZgrade();
        } else {
            
            const errorText = await res.text();
            console.error("Greška sa servera:", errorText);
            Swal.fire('Greška 400', 'Podaci nisu ispravni. Proveri trošak!', 'error');
        }
    } catch (err) {
        console.error("Mreža nije dostupna:", err);
    }
}

// 7. Rešavanje pojedinačnog kvara
async function resiKvar(kId) {
    try {
        const response = await fetch(`${API_URL}/${trenutnaZgradaId}/kvar/${kId}/resi`, { 
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' }
        });

        if (response.ok) {
            
            await osveziListuKvarova(); 
            
           
            await fetchZgrade(); 
            
            Swal.fire({
                icon: 'success',
                title: 'Rešeno!',
                text: 'Budžet je ažuriran!',
                timer: 1500,
                showConfirmButton: false
            });
        }
    } catch (error) {
        console.error("Greška:", error);
    }
}      


async function resiHitne() {
    if (!trenutnaZgradaId) {
        Swal.fire('Greška', 'ID zgrade nije učitan. Zatvori pa ponovo otvori modal.', 'error');
        return;
    }

    
    const base = API_URL.endsWith('/') ? API_URL.slice(0, -1) : API_URL;
    const url = `${base}/${trenutnaZgradaId}/resi-sve-hitne`;

    console.log("Šaljem PATCH na:", url); 
    try {
        const response = await fetch(url, { 
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' }
        });

        if (response.ok) {
            Swal.fire('Uspešno!', 'Svi hitni kvarovi su rešeni.', 'success');
             await osveziListuKvarova(); 
             await fetchZgrade();
        } else {
            
            console.error("404 na putanji:", url);
            Swal.fire('Greška 404', `Server ne vidi ovu putanju. Proveri Swagger rutu!`, 'error');
        }
    } catch (err) {
        console.error("Mrežni problem:", err);
    }
}
// 9. Agregacija (Izveštaj)
function preuzmiIzvestaj() {
    fetch(`${API_URL}/${trenutnaZgradaId}/izvestaj`)
        .then(res => res.json())
        .then(data => {
            console.log("KOMPLEKSNA AGREGACIJA:", data);
            Swal.fire('Agregacija', 'Pogledajte konzolu (F12) za statistiku.', 'info');
        });
}

// UI Pomoćne funkcije
function filterZgrade() {
    const term = document.getElementById('searchZgrada').value.toLowerCase();
    const filtrirane = sveZgrade.filter(z => z.adresa.toLowerCase().includes(term));
    renderZgrade(filtrirane);
}

function showSection(id, el) {
    document.querySelectorAll('.content-section').forEach(s => s.classList.add('hidden'));
    document.getElementById(id).classList.remove('hidden');
    document.querySelectorAll('.nav-links li').forEach(l => l.classList.remove('active'));
    if(el) el.classList.add('active');
}

function closeModal() {
    document.getElementById('modal-kvarovi').classList.add('hidden');
}