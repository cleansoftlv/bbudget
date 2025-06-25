const inMemoryStorage = {};
function isLocalStorageAvailable() {
    try {
        const testKey = '__test__';
        localStorage.setItem(testKey, testKey);
        localStorage.removeItem(testKey);
        return true;
    } catch (e) {
        return false;
    }
}

const storage = isLocalStorageAvailable() ? localStorage : {
    setItem: (key, value) => { inMemoryStorage[key] = value; },
    getItem: (key) => inMemoryStorage[key] || null,
    removeItem: (key) => { delete inMemoryStorage[key]; }
};

export function save(key, value) {
    storage.setItem(key, JSON.stringify(value));
}

export function load(key) {
    const value = storage.getItem(key);
    return value ? JSON.parse(value) : null;
}

export function remove(key) {
    storage.removeItem(key);
}
