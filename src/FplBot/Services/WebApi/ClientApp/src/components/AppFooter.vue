<template>
  <footer class="footer">
    <div class="footer-inner">
      <h4>Love fplbot?</h4>
      <ul>
        <li>Sponsor us on <a href="https://www.patreon.com/johnkors" target="_blank" rel="noreferrer">patreon</a></li>
        <li>Follow us on <a href="https://twitter.com/fplbotapp" target="_blank" rel="noreferrer">twitter</a></li>
        <li>
          Questions:
          <a href="https://join.slack.com/t/fplbot/shared_invite/zt-xgwrkmxw-qNOwrNF9FT5m2kwhOqRhzw" target="_blank" rel="noreferrer">Slack</a>
          /
          <a href="https://discord.gg/zRWMRctR5T" target="_blank" rel="noreferrer">Discord</a>
        </li>
        <li>Other: <a href="mailto:fplbot@blank.no">fplbot@blank.no</a></li>
      </ul>
    </div>
    <p class="fine-print">
      @fplbot is an open source project made by
      <a href="https://www.blank.no/" target="_blank" rel="noreferrer">Blank</a>.
      The code is available on
      <a href="https://github.com/fplbot" target="_blank" rel="noreferrer">github</a>.
    </p>
  </footer>
</template>

<style scoped>
.footer {
  background: var(--fpl-purple);
  color: white;
  padding: 3rem 2rem 1.5rem;
  margin-top: 3rem;
}

.footer-inner {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.footer h4 {
  font-size: 1.125rem;
  font-weight: bold;
  margin-bottom: 0.5rem;
}

.footer ul {
  list-style: none;
  padding: 0;
  line-height: 1.8;
}

.footer a {
  color: white;
  text-decoration: underline;
}

.fine-print {
  color: #d1d5db;
  text-align: center;
  font-size: 0.75rem;
  margin-top: 2rem;
}
</style>
